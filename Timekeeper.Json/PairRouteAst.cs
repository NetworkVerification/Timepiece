using System.Numerics;
using Newtonsoft.Json.Serialization;
using Timekeeper.Json.TypedAst;
using Timekeeper.Json.TypedAst.AstExpr;
using Timekeeper.Json.TypedAst.AstFunction;
using Timekeeper.Json.TypedAst.AstStmt;
using Timekeeper.Networks;
using ZenLib;

namespace Timekeeper.Json;

using Route = Pair<bool, BatfishBgpRoute>;

public class PairRouteAst : Ast<Pair<bool, BatfishBgpRoute>, Unit>
{
  /// <summary>
  /// Default predicates to test for this AST.
  /// </summary>
  public static readonly AstPredicate<Route> IsValid = new("route",
    new First<bool, BatfishBgpRoute, Route>(new Var<Route>("route")));

  /// <summary>
  /// Default import behavior for a route.
  /// </summary>
  private static readonly AstFunction<Route> DefaultImport = AstFunction<Route>.Identity();

  /// <summary>
  /// Default export behavior for a route.
  /// </summary>
  private static readonly AstFunction<Route> DefaultExport = new("arg", new Return<Route>(
    new PairExpr<bool, BatfishBgpRoute, Route>(
      new First<bool, BatfishBgpRoute, Route>(new Var<Route>("arg")),
      new WithField<BatfishBgpRoute, BigInteger, Route>(new Second<bool, BatfishBgpRoute, Route>(new Var<Route>("arg")),
        "AsPathLength",
        new Plus<BigInteger, Route>(
          new GetField<BatfishBgpRoute, BigInteger, Route>(
            new Second<bool, BatfishBgpRoute, Route>(new Var<Route>("arg")),
            "AsPathLength"), new ConstantExpr<BigInteger, Route>(1))))));

  public PairRouteAst(Dictionary<string, NodeProperties<Route>> nodes, Destination? destination,
    Dictionary<string, AstPredicate<Route>> predicates, Dictionary<string, AstPredicate<Unit>> symbolics,
    BigInteger? convergeTime) : base(nodes,
    symbolics, predicates, destination, convergeTime)
  {
  }

  private static Zen<Route> InitFunction(bool isDestination) =>
    Pair.Create<bool, BatfishBgpRoute>(isDestination, new BatfishBgpRoute());

  public Network<Route, Unit> ToNetwork()
  {
    return ToNetwork(InitFunction, BatfishBgpRouteExtensions.MinPair, DefaultExport, DefaultImport);
  }

  public static ISerializationBinder Binder()
  {
    return new AstSerializationBinder<BatfishBgpRoute, Route>();
  }
}
