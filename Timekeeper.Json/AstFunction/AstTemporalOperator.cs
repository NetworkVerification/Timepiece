using System.Numerics;
using ZenLib;

namespace Timekeeper.Json.AstFunction;

public abstract class AstTemporalOperator<T>
{
  public abstract Func<Zen<T>, Zen<BigInteger>, Zen<bool>> Evaluate(Func<string, AstPredicate<T>> getter);
}
