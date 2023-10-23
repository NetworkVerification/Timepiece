using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public class ConjunctionChain : VariadicExpr
{
  public ConjunctionChain(params Expr[] subroutines) : base(subroutines)
  {
  }

  public Environment<RouteEnvironment> Evaluate(AstEnvironment astEnv, Environment<RouteEnvironment> env)
  {
    // add the default policy at the end of the chain
    var policies = astEnv.defaultPolicy is not null ? Exprs.Append(new Call(astEnv.defaultPolicy)) : Exprs;
    // go through the policies in reverse order to produce the final environment
    // we start with false as the return value as a default, but the default policy should never fall through
    var acc = new Environment<RouteEnvironment>(env.route.WithResultFallthrough(true), Zen.False());
    // each policy may update the route, so the policy routes need to be computed in sequential order
    var policyResults = new List<Environment<RouteEnvironment>>();
    var lastEnv = env.WithRoute(env.route.WithResultFallthrough(true));
    foreach (var policy in policies)
    {
      var subroutineResult = astEnv.EvaluateExpr(lastEnv, policy);
      policyResults.Add(subroutineResult);
      lastEnv = subroutineResult;
    }

    for (var i = policyResults.Count - 1; i > 0; i--)
    {
      // Logic of subroutines:
      // (1) if the subroutine exits, the result will be that subroutine
      // (2) if the subroutine falls through OR returns true, the result will be the following route
      var fallthroughGuard = Zen.Or(policyResults[i].route.GetResultFallthrough(),
        policyResults[i].route.GetResultValue());
      var exitGuard = policyResults[i].route.GetResultExit();
      var accRoute = Zen.If(exitGuard, policyResults[i].route,
        Zen.If(fallthroughGuard, acc.route, policyResults[i].route));
      var accResult = Zen.If(exitGuard, policyResults[i].returnValue,
        (dynamic?) Zen.If(fallthroughGuard, acc.returnValue, policyResults[i].returnValue));
      acc = policyResults[i].WithRoute(accRoute).WithValue(accResult);
    }

    return acc;
  }
}
