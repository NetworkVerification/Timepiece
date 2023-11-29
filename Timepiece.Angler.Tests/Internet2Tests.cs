using Newtonsoft.Json;
using Timepiece.Angler.Ast;
using Timepiece.Angler.DataTypes;
using Timepiece.Angler.Networks;
using Timepiece.DataTypes;
using Timepiece.Tests;
using Xunit.Abstractions;
using ZenLib;

namespace Timepiece.Angler.Tests;

public class Internet2Tests
{
  private readonly ITestOutputHelper _testOutputHelper;
  private const string Internet2FileName = "INTERNET2.angler.json";

  // TODO: change this to instead track the file down by going up the directories
  private static readonly string Internet2Path =
    Path.Join(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.Parent!.ToString(),
      Internet2FileName);

  private static readonly AnglerNetwork Internet2Ast =
    AstSerializationBinder.JsonSerializer()
      .Deserialize<AnglerNetwork>(new JsonTextReader(new StreamReader(Internet2Path)))!;

  private static readonly NodeProperties WashProperties = Internet2Ast.Nodes["wash"];

  /// <summary>
  /// Nodes whose incoming routes (to Internet2 nodes) are always rejected.
  /// </summary>
  private static readonly IEnumerable<string> RejectIncoming = Internet2Nodes.OtherGroup
    .Concat(Internet2Nodes.OtherInternalGroup)
    .Concat(Internet2Nodes.AdvancedLayer2ServiceManagementGroup);

  public Internet2Tests(ITestOutputHelper testOutputHelper)
  {
    _testOutputHelper = testOutputHelper;
  }

  [Fact]
  public void Internet2JsonFileExists()
  {
    _testOutputHelper.WriteLine(Internet2Path);
    Assert.True(File.Exists(Internet2Path));
  }

  [Fact]
  public void TransferNoMartians()
  {
    var (topology, transfer) = Internet2Ast.TopologyAndTransfer(trackTerms: true);
    var externalNodes = Internet2Ast.Externals.Select(i => i.Name);
    var route = Zen.Symbolic<RouteEnvironment>("r");
    // iterate over the edges from neighbors to Internet2 nodes
    // explicitly exclude the "10.11.1.17"-->"newy-re1" edge, which does not have a SANITY-IN
    var externalToInternal = topology.Edges(e =>
      e != ("10.11.1.17", "newy-re1") && externalNodes.Contains(e.Item1) &&
      Internet2Nodes.AsNodes.Contains(e.Item2));
    Assert.All(externalToInternal, edge =>
    {
      var transferCheck = new TransferCheck<RouteEnvironment>(transfer[edge]);
      var result = transferCheck.Verify(route, r => r.GetPrefix().IsValidPrefixLength(),
        r => Zen.Implies(r.GetResultValue(),
          AnglerInternet2.NoPrefixMatch(AnglerInternet2.MartianPrefixes, r.GetPrefix())));
      _testOutputHelper.WriteLine($"Edge {edge}: {result}");
      Assert.Null(result);
    });
  }

  /// <summary>
  /// Verify that the given route is "good":
  /// <list type="bullet">
  ///   <item>its result value is true and other result features are false</item>
  ///   <item>its AS set is empty</item>
  ///   <item>its prefix is 35.0.0.0/8 (UMichigan's prefix)</item>
  /// </list>
  /// </summary>
  /// <param name="route"></param>
  /// <returns></returns>
  private static Zen<bool> IsGoodUMichRoute(Zen<RouteEnvironment> route) => Zen.And(
    route.GetResultValue(),
    route.GetLocalDefaultAction(),
    route.NonTerminated(),
    Zen.Not(route.GetAsSet().Contains(AnglerInternet2.NlrAs)),
    Zen.Not(route.GetAsSet().Contains(AnglerInternet2.PrivateAs)),
    Zen.Not(route.GetAsSet().Contains(AnglerInternet2.CommercialAs)),
    route.GetPrefix() == new Ipv4Prefix("35.0.0.0", "35.255.255.255"));

  [Fact]
  public void WashSanityInAcceptsGoodRoute()
  {
    var sanityIn = WashProperties.Declarations["SANITY-IN"];
    // the call expression context must be true in order for SANITY-IN not to Return when it completes
    var import = new AstState(WashProperties.Declarations, callExprContext: true).EvaluateFunction(sanityIn);
    var transfer = new TransferCheck<RouteEnvironment>(import);
    var result = transfer.Verify(Zen.Symbolic<RouteEnvironment>("r"), IsGoodUMichRoute, r => r.GetResultValue());
    Assert.Null(result);
  }

  [Fact]
  public void WashNeighborImportAcceptsGoodRoute()
  {
    var importPolicy = WashProperties.Policies["192.122.183.13"].Import!;
    var importFunction = WashProperties.Declarations[importPolicy];
    var import = new AstState(WashProperties.Declarations).EvaluateFunction(importFunction);
    // This import function does the following:
    // 1. Set the default policy to ~DEFAULT_BGP_IMPORT_POLICY~
    // 2. Set LocalDefaultAction to true.
    // 3. Perform a FirstMatchChain on policies SANITY-IN, SET-PREF, MERIT-IN, CONNECTOR-IN
    // 4. If the FirstMatchChain returns true, assign the result to Exit=true,Value=true
    //    Otherwise if it returns false, assign the result to Exit=true,Value=false
    var transfer = new TransferCheck<RouteEnvironment>(import);
    var result = transfer.Verify(Zen.Symbolic<RouteEnvironment>("r"), IsGoodUMichRoute,
      imported => Zen.And(imported.GetResultValue(), imported.GetResultExit()));
    Assert.Null(result);
  }

  /// <summary>
  /// Verify that every edge that goes into an Internet2 node accepts some route.
  /// </summary>
  [Fact]
  public void Internet2TransferAccepts()
  {
    var (topology, transfer) = Internet2Ast.TopologyAndTransfer();
    var route = Zen.Symbolic<RouteEnvironment>("r");
    // iterate over the edges from non-rejected neighbors to Internet2 nodes
    var acceptedEdges = topology.Edges(e =>
      !RejectIncoming.Contains(e.Item1) && Internet2Nodes.AsNodes.Contains(e.Item2));
    // check all the accepted edges
    Assert.All(acceptedEdges, edge =>
    {
      var transferCheck = new TransferCheck<RouteEnvironment>(transfer[edge]);
      var result = transferCheck.Solve(route, r => r.GetPrefix().IsValidPrefixLength(), r => r.GetResultValue());
      _testOutputHelper.WriteLine($"Edge {edge}: {result}");
      Assert.NotNull(result);
    });
  }

  [Fact]
  public void WashMeritNeighborTransferAcceptsGoodRoute()
  {
    var (_, transfer) = Internet2Ast.TopologyAndTransfer();
    // export + import
    var transferCheck = new TransferCheck<RouteEnvironment>(transfer[("192.122.183.13", "wash")]);
    var result = transferCheck.Verify(Zen.Symbolic<RouteEnvironment>("r"), IsGoodUMichRoute, r => r.GetResultValue());
    Assert.Null(result);
  }

  [Fact]
  public void WashCaarenNeighborTransfersGoodRoute()
  {
    var route = Zen.Symbolic<RouteEnvironment>("r");
    // var route = new RouteEnvironment
    // {
    // Result = new RouteResult {Exit = false, Fallthrough = false, Returned = false, Value = true},
    // LocalDefaultAction = false, Prefix = new Ipv4Prefix("128.164.0.0", "128.164.255.255"), Weight = 0, Lp = 0,
    // AsSet = new CSet<string>(),
    // AsPathLength = 0, Metric = 0, OriginType = new UInt<_2>(0), Tag = 0, Communities =
    // new CSet<string>(),
    // VisitedTerms = new CSet<string>()
    // };
    var (_, transfer) = Internet2Ast.TopologyAndTransfer(trackTerms: true);
    var transferCheck = new TransferCheck<RouteEnvironment>(transfer[("198.71.45.247", "wash")]);
    var result = transferCheck.Verify(route,
      r => Zen.And(r.NonTerminated(), r.GetResultValue(),
        r.GetPrefix() == new Ipv4Prefix("128.164.0.0", "128.164.255.255"),
        Zen.Not(r.GetAsSet().Contains(AnglerInternet2.PrivateAs)),
        Zen.Not(r.GetAsSet().Contains(AnglerInternet2.CommercialAs)),
        Zen.Not(r.GetAsSet().Contains(AnglerInternet2.NlrAs))),
      r => r.GetResultValue());
    _testOutputHelper.WriteLine($"{result}");
    Assert.Null(result);
  }

  [Fact]
  public void WashNeighborTransferRejectsAsSetRoute()
  {
    var (_, transfer) = Internet2Ast.TopologyAndTransfer();
    var transferCheck = new TransferCheck<RouteEnvironment>(transfer[("192.122.183.13", "wash")]);
    // _testOutputHelper.WriteLine(transferCheck.Transfer(Zen.Symbolic<RouteEnvironment>("route")).Format());
    var result = transferCheck.Verify(Zen.Symbolic<RouteEnvironment>("r"),
      // constrain the route to have an AsSet element that forces it to be filtered
      route =>
        Zen.Or(route.GetAsSet().Contains(AnglerInternet2.NlrAs),
          route.GetAsSet().Contains(AnglerInternet2.PrivateAs),
          route.GetAsSet().Contains(AnglerInternet2.CommercialAs)),
      r => Zen.Not(r.GetResultValue()));
    Assert.Null(result);
  }

  [Fact]
  public void HousNeighborRejectsPrivateRoute()
  {
    var (_, transfer) = Internet2Ast.TopologyAndTransfer();
    // 64.57.28.149 is the [NETPLUS] Level(3) IP SIP Commodity | I2-S08834 neighbor
    var transferCheck = new TransferCheck<RouteEnvironment>(transfer[("64.57.28.149", "hous")]);
    var result = transferCheck.Verify(Zen.Symbolic<RouteEnvironment>("r"),
      // constrain the route to be from a private AS
      r => r.GetAsSet().Contains(AnglerInternet2.PrivateAs),
      r => Zen.Not(r.GetResultValue()));
    Assert.Null(result);
  }

  [Fact]
  public void SomeNeighborAcceptsAsSetRoute()
  {
    var (topology, transfer) = Internet2Ast.TopologyAndTransfer(trackTerms: true);
    // iterate over the edges from non-rejected neighbors to Internet2 nodes
    var acceptedEdges = topology.Edges(e =>
      !RejectIncoming.Contains(e.Item1) && Internet2Nodes.AsNodes.Contains(e.Item2));
    Assert.Contains(acceptedEdges, edge =>
    {
      var transferCheck = new TransferCheck<RouteEnvironment>(transfer[edge]);
      var result = transferCheck.Solve(Zen.Symbolic<RouteEnvironment>("r"),
        r => Zen.And(r.GetResultValue(),
          r.GetLocalDefaultAction(),
          r.NonTerminated(),
          // has one of the filtered AsSet elements
          Zen.Or(r.GetAsSet().Contains(AnglerInternet2.NlrAs),
            r.GetAsSet().Contains(AnglerInternet2.PrivateAs),
            r.GetAsSet().Contains(AnglerInternet2.CommercialAs)),
          r.GetPrefix().IsValidPrefixLength()),
        r => r.GetResultValue());
      return result is not null;
    });
  }

  [Fact(Skip = "too slow")]
  public void WashReachableInductiveCheckPasses()
  {
    var (topology, transfer) = Internet2Ast.TopologyAndTransfer();
    var externalNodes = Internet2Ast.Externals.Select(i => i.Name);
    var net = AnglerInternet2.Reachable(topology, externalNodes, transfer);
    NetworkAsserts.Sound(net, SmtCheck.Inductive, "wash");
  }

  [Fact(Skip = "too slow")]
  public void Internet2ReachableMonolithic()
  {
    var (topology, transfer) = Internet2Ast.TopologyAndTransfer();
    var externalNodes = Internet2Ast.Externals.Select(i => i.Name);
    var net = AnglerInternet2.Reachable(topology, externalNodes, transfer);
    NetworkAsserts.Sound(net, SmtCheck.Monolithic);
  }

  [Theory]
  [InlineData(SmtCheck.Monolithic, Skip = "too slow")]
  [InlineData(SmtCheck.Modular)]
  public void Internet2BadPropertyFails(SmtCheck check)
  {
    var (topology, transfer) = Internet2Ast.TopologyAndTransfer();
    var routes = SymbolicValue.SymbolicDictionary<string, RouteEnvironment>("route", topology.Nodes);
    var initialRoutes = topology.MapNodes(n => routes[n].Value);
    var monolithicProperties = topology.MapNodes(_ => Lang.False<RouteEnvironment>());
    var modularProperties = topology.MapNodes(n => Lang.Globally(monolithicProperties[n]));
    var net = new AnglerInternet2(topology, transfer, RouteEnvironmentExtensions.MinOptional, initialRoutes,
      modularProperties, modularProperties, monolithicProperties, routes.Values.Cast<ISymbolic>().ToArray());
    NetworkAsserts.Unsound(net, check);
  }
}
