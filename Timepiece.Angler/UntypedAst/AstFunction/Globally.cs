using System.Numerics;
using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstFunction;

public class Globally<T> : AstTemporalOperator<T>
{
  public Globally(string p)
  {
    Predicate = p;
  }

  public string Predicate { get; }

  public override Func<Zen<T>, Zen<BigInteger>, Zen<bool>> Evaluate(Func<string, AstPredicate<T>> getter)
  {
    var f = getter(Predicate).Evaluate(new AstEnvironment());
    return Lang.Globally(f);
  }

  public override string ToString()
  {
    return $"Globally({Predicate})";
  }
}