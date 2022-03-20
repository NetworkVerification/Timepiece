using System.Numerics;
using Gardener.AstExpr;
using ZenLib;

namespace Gardener.AstFunction;

public class Finally<T> : AstPredicate<Pair<T, BigInteger>>
{
  public BigInteger Time { get; set; }

  public Finally(BigInteger time, AstPredicate<Pair<T, BigInteger>> predicate) : base(predicate.Arg, Wrap(predicate, time))
  {
    Time = time;
  }

  private static Expr<bool, Pair<T, BigInteger>> Wrap(AstPredicate<Pair<T, BigInteger>> predicate, BigInteger time)
  {
    var a = new Var<Pair<T, BigInteger>>(predicate.Arg);
    var t = new ConstantExpr<BigInteger, Pair<T, BigInteger>>(time);
    // true if the second element of a < time
    var beforeBranch =
      new LessThan<BigInteger, Pair<T, BigInteger>>(new Second<T, BigInteger, Pair<T, BigInteger>>(a), t);
    return new Or<Pair<T, BigInteger>>(beforeBranch, predicate.Body);
  }
}
