using Timepiece.Angler.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

/// <summary>
///   See Batfish's ConjunctionChain expression.
/// </summary>
public record ConjunctionChain : VariadicExpr
{
  public ConjunctionChain(params Expr[] subroutines) : base(subroutines)
  {
  }

  public ReturnRoute<RouteEnvironment> Evaluate(AstState astEnv,
    ReturnRoute<RouteEnvironment> returnEnv)
  {
    // add the default policy at the end of the chain
    var policies = astEnv.DefaultPolicy is not null ? Exprs.Append(new Call(astEnv.DefaultPolicy)) : Exprs;
    // go through the policies in reverse order to produce the final environment
    // we start with false as the return value as a default, but the default policy should never fall through
    var acc = new ReturnRoute<RouteEnvironment>(returnEnv.Route.WithResultFallthrough(true), Zen.False());
    // each policy may update the route, so the policy routes need to be computed in sequential order
    var policyResults = new List<ReturnRoute<RouteEnvironment>>();
    var lastReturn = returnEnv with {Route = returnEnv.Route.WithResultFallthrough(true)};
    foreach (var policy in policies)
    {
      var subroutineResult = astEnv.EvaluateExpr(lastReturn, policy);
      policyResults.Add(subroutineResult);
      lastReturn = subroutineResult;
    }

    for (var i = policyResults.Count - 1; i > 0; i--)
    {
      // Logic of subroutines:
      // (1) if the subroutine exits, the result will be that subroutine
      // (2) if the subroutine falls through OR returns true, the result will be the following route
      var fallthroughGuard = Zen.Or(policyResults[i].Route.GetResultFallthrough(),
        policyResults[i].Route.GetResultValue());
      var exitGuard = policyResults[i].Route.GetResultExit();
      var accRoute = Zen.If(exitGuard, policyResults[i].Route,
        Zen.If(fallthroughGuard, acc.Route, policyResults[i].Route));
      var accResult = Zen.If(exitGuard, policyResults[i].ReturnValue,
        (dynamic?) Zen.If(fallthroughGuard, acc.ReturnValue, policyResults[i].ReturnValue));
      ReturnRoute<RouteEnvironment> tempQualifier = (policyResults[i] with {Route = accRoute});
      acc = tempQualifier with {ReturnValue = accResult};
    }

    return acc;
  }
}
