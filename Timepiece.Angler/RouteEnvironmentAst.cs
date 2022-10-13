using System.Numerics;
using Newtonsoft.Json.Serialization;
using Timepiece.Angler.UntypedAst;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstFunction;
using Timepiece.Angler.UntypedAst.AstStmt;
using Timepiece.Datatypes;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Angler;

public class RouteEnvironmentAst : Ast<RouteEnvironment, Unit>
{
  /// <summary>
  /// Default predicates to test for this AST.
  /// </summary>
  public static readonly AstPredicate<RouteEnvironment> IsValid = new("route",
    new First(new Var("route")));

  /// <summary>
  /// Default import behavior for a route.
  /// </summary>
  private static readonly AstFunction<RouteEnvironment> DefaultImport = AstFunction<RouteEnvironment>.Identity();

  /// <summary>
  /// Default export behavior for a route.
  /// </summary>
  private static readonly AstFunction<RouteEnvironment> DefaultExport = new("arg", new[]
  {
    new Return(
      new PairExpr(
        new First(new Var("arg")),
        new WithField(new Second(new Var("arg")),
          "AsPathLength",
          new Plus(
            new GetField(typeof(BatfishBgpRoute), typeof(BigInteger),
              new Second(new Var("arg")),
              "AsPathLength"), new ConstantExpr(BigInteger.One)))))
  });

  public RouteEnvironmentAst(Dictionary<string, UntypedAst.NodeProperties<RouteEnvironment>> nodes,
    Ipv4Prefix? destination,
    Dictionary<string, AstPredicate<RouteEnvironment>> predicates, Dictionary<string, AstPredicate<Unit>> symbolics,
    BigInteger? convergeTime) : base(nodes,
    symbolics, predicates, destination, convergeTime)
  {
  }

  public Network<RouteEnvironment, Unit> ToNetwork()
  {
    return ToNetwork(RouteEnvironmentExtensions.MinOptional, DefaultExport, DefaultImport);
  }

  public static ISerializationBinder Binder()
  {
    return new AstSerializationBinder<RouteEnvironment>();
  }

  public static IContractResolver Resolver()
  {
    return new AstContractResolver();
  }
}
