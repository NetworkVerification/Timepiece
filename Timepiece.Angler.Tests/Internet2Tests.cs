using Newtonsoft.Json;
using Timepiece.Angler.Ast;
using Timepiece.DataTypes;
using Xunit.Abstractions;
using ZenLib;

namespace Timepiece.Angler.Tests;

public class Internet2Tests
{
  private readonly ITestOutputHelper _testOutputHelper;
  private const string Internet2Path = "/Users/tim/Documents/Princeton/envy/angler/internet2.angler.json";

  private static readonly JsonSerializer Serializer = new JsonSerializer
  {
    TypeNameHandling = TypeNameHandling.All,
    SerializationBinder = new AstSerializationBinder()
  };

  private static readonly AnglerNetwork Internet2Ast =
    Serializer.Deserialize<AnglerNetwork>(new JsonTextReader(new StreamReader(Internet2Path)))!;

  private static readonly NodeProperties WashProperties = Internet2Ast.Nodes["wash"];

  public Internet2Tests(ITestOutputHelper testOutputHelper)
  {
    _testOutputHelper = testOutputHelper;
  }

  [Fact]
  public void WashSanityInAcceptsGoodRoute()
  {
    var assumptions = new Func<Zen<RouteEnvironment>, Zen<bool>>(route => Zen.And(route.GetResultValue(),
      route.GetLocalDefaultAction(), Zen.Not(route.GetResultReturned()),
      Zen.Not(route.GetResultExit()),
      Zen.Not(route.GetResultFallthrough()), route.GetAsSet().IsSubsetOf(CSet.Empty<string>()),
      route.GetPrefix() == new Ipv4Prefix("35.0.0.0", "35.255.255.255")));
    var sanityIn = WashProperties.Declarations["SANITY-IN"];
    var import = new AstEnvironment(WashProperties.Declarations).WithCallExprContext(true)
      .EvaluateFunction(sanityIn);
    var transfer = new TransferCheck<RouteEnvironment>(import);
    var result = transfer.Check(Zen.Symbolic<RouteEnvironment>("r"), assumptions, r => r.GetResultValue());
    Assert.Null(result);
  }

  [Fact]
  public static void WashNeighborImportAcceptsGoodRoute()
  {
    var assumptions = new Func<Zen<RouteEnvironment>, Zen<bool>>(route => Zen.And(route.GetResultValue(),
      Zen.Not(route.GetResultReturned()), Zen.Not(route.GetResultExit()),
      Zen.Not(route.GetResultFallthrough()), route.GetAsSet().IsSubsetOf(CSet.Empty<string>()),
      route.GetPrefix() == new Ipv4Prefix("35.0.0.0", "35.255.255.255")));
    var importPolicy = WashProperties.Policies["192.122.183.13"].Import!;
    var importFunction = WashProperties.Declarations[importPolicy];
    var import = new AstEnvironment(WashProperties.Declarations).EvaluateFunction(importFunction);
    // This import function does the following:
    // 1. Set the default policy to ~DEFAULT_BGP_IMPORT_POLICY~
    // 2. Set LocalDefaultAction to true.
    // 3. Perform a FirstMatchChain on policies SANITY-IN, SET-PREF, MERIT-IN, CONNECTOR-IN
    // 4. If the FirstMatchChain returns true, assign the result to Exit=true,Value=true
    //    Otherwise if it returns false, assign the result to Exit=true,Value=false
    var transfer = new TransferCheck<RouteEnvironment>(import);
    var result = transfer.Check(Zen.Symbolic<RouteEnvironment>("r"), assumptions,
      imported => Zen.And(imported.GetResultValue(), imported.GetResultExit()));
    Assert.Null(result);
  }

  [Fact]
  public void WashNeighborTransferAcceptsGoodRoute()
  {
    var (_, transfer) = Internet2Ast.TopologyAndTransfer();
    // This import function does the following:
    // 1. Set the default policy to ~DEFAULT_BGP_IMPORT_POLICY~
    // 2. Set LocalDefaultAction to true.
    // 3. Perform a FirstMatchChain on policies SANITY-IN, SET-PREF, MERIT-IN, CONNECTOR-IN
    // 4. If the FirstMatchChain returns true, assign the result to Exit=true,Value=true
    //    Otherwise if it returns false, assign the result to Exit=true,Value=false
    var transferCheck = new TransferCheck<RouteEnvironment>(transfer[("192.122.183.13", "wash")]);
    _testOutputHelper.WriteLine(transferCheck.Transfer(Zen.Symbolic<RouteEnvironment>("route")).Format());
    var result = transferCheck.Check(Zen.Symbolic<RouteEnvironment>("r"),
      r => Zen.And(r.GetResultValue(),
        Zen.Not(r.GetResultReturned()),
        Zen.Not(r.GetResultExit()),
        Zen.Not(r.GetResultFallthrough()), r.GetAsSet().IsSubsetOf(CSet.Empty<string>()),
        r.GetPrefix() == new Ipv4Prefix("35.0.0.0", "35.255.255.255")),
      r => r.GetResultValue());
    Assert.Null(result);
  }
}
