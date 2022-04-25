using System.Numerics;
using ZenLib;

namespace Gardener.AstFunction;

public abstract class AstTemporalOperator<T>
{
  public abstract Func<Zen<T>, Zen<BigInteger>, Zen<bool>> Evaluate(Func<string, AstPredicate<T>> getter);
}
