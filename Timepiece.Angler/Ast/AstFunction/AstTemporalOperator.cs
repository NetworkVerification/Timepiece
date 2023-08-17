using System.Numerics;
using ZenLib;

namespace Timepiece.Angler.Ast.AstFunction;

public abstract class AstTemporalOperator
{
  public abstract Func<Zen<RouteEnvironment>, Zen<BigInteger>, Zen<bool>> Evaluate(
    Func<string, AstPredicate> getter,
    Dictionary<string, AstFunction<RouteEnvironment>> declarations);
}
