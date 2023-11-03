using Newtonsoft.Json;
using Timepiece.Angler.Ast;
using Timepiece.Angler.DataTypes;
using Timepiece.Angler.Queries;
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

  /// <summary>
  /// Check that the given route is "good":
  /// <list type="bullet">
  ///   <item>its result value is true and other result features are false</item>
  ///   <item>its AS set is empty</item>
  ///   <item>its prefix is 35.0.0.0/8</item>
  /// </list>
  /// </summary>
  /// <param name="route"></param>
  /// <returns></returns>
  private static Zen<bool> IsGoodRoute(Zen<RouteEnvironment> route) => Zen.And(
    route.GetResultValue(),
    route.GetLocalDefaultAction(),
    route.NonTerminated(),
    Zen.Not(route.GetAsSet().Contains(Internet2.NlrAs)),
    Zen.Not(route.GetAsSet().Contains(Internet2.PrivateAs)),
    Zen.Not(route.GetAsSet().Contains(Internet2.CommercialAs)),
    route.GetPrefix() == new Ipv4Prefix("35.0.0.0", "35.255.255.255"));

  [Fact]
  public void WashSanityInAcceptsGoodRoute()
  {
    var sanityIn = WashProperties.Declarations["SANITY-IN"];
    // the call expression context must be true in order for SANITY-IN not to Return when it completes
    var import = new AstState(WashProperties.Declarations, callExprContext: true).EvaluateFunction(sanityIn);
    var transfer = new TransferCheck<RouteEnvironment>(import);
    var result = transfer.Check(Zen.Symbolic<RouteEnvironment>("r"), IsGoodRoute, r => r.GetResultValue());
    Assert.Null(result);
  }

  [Fact]
  public static void WashNeighborImportAcceptsGoodRoute()
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
    var result = transfer.Check(Zen.Symbolic<RouteEnvironment>("r"), IsGoodRoute,
      imported => Zen.And(imported.GetResultValue(), imported.GetResultExit()));
    Assert.Null(result);
  }

  /// <summary>
  /// Check that every edge that goes into an Internet2 node accepts some route.
  /// </summary>
  [Fact]
  public void Internet2TransferAccepts()
  {
    var (topology, transfer) = Internet2Ast.TopologyAndTransfer();
    var route = Zen.Symbolic<RouteEnvironment>("r");
    // neighbors whose incoming routes are always rejected
    var rejectIncoming = Internet2.OtherGroup
      .Concat(Internet2.OtherInternalGroup)
      .Concat(Internet2.AdvancedLayer2ServiceManagementGroup);
    // iterate over the edges from non-rejected neighbors to Internet2 nodes
    var acceptedEdges = topology.Edges(e =>
      !rejectIncoming.Contains(e.Item1) && Internet2.Internet2Nodes.Contains(e.Item2));
    // check all the accepted edges
    Assert.All(acceptedEdges, edge =>
    {
      var transferred = transfer[edge](route);
      var model = Zen.And(route.GetPrefix().IsValidPrefixLength(), transferred.GetResultValue()).Solve();
      (RouteEnvironment, RouteEnvironment)? result =
        model.IsSatisfiable() ? (model.Get(route), model.Get(transferred)) : null;
      _testOutputHelper.WriteLine(result.HasValue
        ? $"Edge {edge}: {result.Value.Item1} --> {result.Value.Item2}"
        : $"Edge {edge}: null");
      Assert.NotNull(result);
    });
  }

  [Fact]
  public void WashNeighborTransferAcceptsGoodRoute()
  {
    var (_, transfer) = Internet2Ast.TopologyAndTransfer();
    // export + import
    var transferCheck = new TransferCheck<RouteEnvironment>(transfer[("192.122.183.13", "wash")]);
    var result = transferCheck.Check(Zen.Symbolic<RouteEnvironment>("r"), IsGoodRoute, r => r.GetResultValue());
    Assert.Null(result);
  }

  [Fact]
  public void WashNeighborTransferRejectsBadRoute()
  {
    var (_, transfer) = Internet2Ast.TopologyAndTransfer();
    // export + import
    var transferCheck = new TransferCheck<RouteEnvironment>(transfer[("192.122.183.13", "wash")]);
    _testOutputHelper.WriteLine(transferCheck.Transfer(Zen.Symbolic<RouteEnvironment>("route")).Format());
    var result = transferCheck.Check(Zen.Symbolic<RouteEnvironment>("r"),
      // constrain the route to have an AsSet element that forces it to be filtered
      route => Zen.And(route.GetResultValue(),
        route.GetLocalDefaultAction(),
        route.NonTerminated(),
        Zen.Or(route.GetAsSet().Contains(Internet2.NlrAs), route.GetAsSet().Contains(Internet2.PrivateAs),
          route.GetAsSet().Contains(Internet2.CommercialAs)),
        route.GetPrefix() == new Ipv4Prefix("35.0.0.0", "35.255.255.255")), r => Zen.Not(r.GetResultValue()));
    Assert.Null(result);
  }

  [Fact]
  public void WashReachableInductiveCheckPasses()
  {
    var (topology, transfer) = Internet2Ast.TopologyAndTransfer();
    var externalNodes = Internet2Ast.Externals.Select(i => i.Name);
    var net = Internet2.Reachable(topology, externalNodes)
      .ToNetwork(topology, transfer, RouteEnvironmentExtensions.MinOptional);
    NetworkAsserts.Sound(net, SmtCheck.Inductive, "wash");
  }

  [Fact]
  public void Internet2ReachableMonolithic()
  {
    var (topology, transfer) = Internet2Ast.TopologyAndTransfer();
    var externalNodes = Internet2Ast.Externals.Select(i => i.Name);
    var net = Internet2.Reachable(topology, externalNodes)
      .ToNetwork(topology, transfer, RouteEnvironmentExtensions.MinOptional);
    NetworkAsserts.Sound(net, SmtCheck.Monolithic);
  }
}
