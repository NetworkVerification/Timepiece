using Gardener.AstExpr;
using Gardener.AstFunction;
using Gardener.AstStmt;
using Karesansui.Networks;
using Newtonsoft.Json.Serialization;
using ZenLib;

namespace Gardener;

using Route = Pair<bool, BatfishBgpRoute>;

public class PairRouteAst : Ast<Route, Unit>
{
  /// <summary>
  /// Default predicates to test for this AST.
  /// </summary>
  public new static readonly AstPredicate<Route> IsValid = new("route",
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
      new WithField<BatfishBgpRoute, int, Route>(new Second<bool, BatfishBgpRoute, Route>(new Var<Route>("arg")),
        "AsPathLength",
        new Plus<int, Route>(
          new GetField<BatfishBgpRoute, int, Route>(
            new Second<bool, BatfishBgpRoute, Route>(new Var<Route>("arg")),
            "AsPathLength"), new ConstantExpr<int, Route>(1))))));

  public PairRouteAst(Dictionary<string, NodeProperties<Route>> nodes, Destination? destination) : base(nodes,
    new Dictionary<string, AstPredicate<Unit>>(), new Dictionary<string, AstPredicate<Route>>
    {
      {"IsValid", IsValid}
    }, destination)
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
