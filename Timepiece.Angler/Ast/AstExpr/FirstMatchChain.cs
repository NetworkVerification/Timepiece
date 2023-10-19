using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public class FirstMatchChain : VariadicExpr
{
  public FirstMatchChain(params Expr[] subroutines) : base(subroutines)
  {
  }

  public Environment<RouteEnvironment> Evaluate(AstEnvironment astEnv, Environment<RouteEnvironment> env)
  {
    if (astEnv.defaultPolicy is null)
      throw new Exception("Default policy not set!");
    // add the default policy at the end of the chain
    var policies = Exprs.Append(new Call(astEnv.defaultPolicy));
    // each policy may update the route, so the policy routes need to be computed in sequential order
    var policyResults = new List<Environment<RouteEnvironment>>();
    var lastEnv = env;
    foreach (var policy in policies)
    {
      var subroutineResult = astEnv.EvaluateExpr(lastEnv, policy);
      policyResults.Add(subroutineResult);
      lastEnv = subroutineResult;
    }

    // go through the policies in reverse order to produce the final environment
    // we start with the local default action as the return value as a default
    var acc = env.WithValue(env.route.GetLocalDefaultAction());
    for (var i = policyResults.Count - 1; i >= 0; i--)
    {
      // Logic of subroutines:
      // (1) if the subroutine exits, the result will be that subroutine
      // (2) if the subroutine falls through, the result will be the following route
      var fallthroughGuard = policyResults[i].route.GetResult().GetFallthrough();
      var exitGuard = policyResults[i].route.GetResult().GetExit();
      var accRoute = Zen.If(exitGuard, policyResults[i].route,
        Zen.If(fallthroughGuard, acc.route, policyResults[i].route));
      var accResult = Zen.If(exitGuard, policyResults[i].returnValue,
        Zen.If(fallthroughGuard, acc.returnValue, policyResults[i].returnValue));
      acc = new Environment<RouteEnvironment>(accRoute, accResult);
    }

    return acc;
  }
}
