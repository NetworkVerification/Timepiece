using Timepiece.Angler.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

/// <summary>
///   See Batfish's FirstMatchChain.
/// </summary>
public class FirstMatchChain : VariadicExpr
{
  public FirstMatchChain(params Expr[] subroutines) : base(subroutines)
  {
  }

  public ReturnRoute<RouteEnvironment> Evaluate(AstState astEnv, ReturnRoute<RouteEnvironment> env)
  {
    if (astEnv.DefaultPolicy is null)
      throw new Exception("Default policy not set!");
    // add the default policy at the end of the chain
    var policies = Exprs.Append(new Call(astEnv.DefaultPolicy));
    // each policy may update the route, so the policy routes need to be computed in sequential order
    var policyResults = new List<ReturnRoute<RouteEnvironment>>();
    var lastEnv = env;
    foreach (var policy in policies)
    {
      var subroutineResult = astEnv.EvaluateExpr(lastEnv, policy);
      policyResults.Add(subroutineResult);
      lastEnv = subroutineResult;
    }

    // go through the policies in reverse order to produce the final environment
    // we start with the local default action as the return value as a default
    dynamic returnValue = env.Route.GetLocalDefaultAction();
    var acc = env with {ReturnValue = returnValue};
    for (var i = policyResults.Count - 1; i >= 0; i--)
    {
      // Logic of subroutines:
      // (1) if the subroutine exits, the result will be that subroutine
      // (2) if the subroutine falls through, the result will be the following route
      var fallthroughGuard = policyResults[i].Route.GetResultFallthrough();
      var exitGuard = policyResults[i].Route.GetResultExit();
      var accRoute = Zen.If(exitGuard, policyResults[i].Route,
        Zen.If(fallthroughGuard, acc.Route, policyResults[i].Route));
      var accResult = Zen.If(exitGuard, policyResults[i].ReturnValue,
        Zen.If(fallthroughGuard, acc.ReturnValue, policyResults[i].ReturnValue));
      acc = new ReturnRoute<RouteEnvironment>(accRoute, accResult);
    }

    return acc;
  }
}
