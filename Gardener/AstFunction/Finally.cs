using System.Numerics;
using Gardener.AstExpr;
using ZenLib;

namespace Gardener.AstFunction;

public class Finally<T> : AstPredicate<Pair<T, BigInteger>>
{
  public BigInteger Time { get; set; }

  public Finally(string arg, BigInteger time, AstPredicate<T> pred) : base(arg, Wrap(arg, pred, time))
  {
    Time = time;
  }

  private static Expr<bool, Pair<T, BigInteger>> Wrap(string arg, AstPredicate<T> pred, BigInteger time)
  {
    var a = new Var<Pair<T, BigInteger>>(arg);
    var t = new ConstantExpr<BigInteger, Pair<T, BigInteger>>(time);
    // true if the second element of a < time
    var beforeBranch =
      new LessThan<BigInteger, Pair<T, BigInteger>>(new Second<T, BigInteger, Pair<T, BigInteger>>(a), t);
    // true if the predicate holds for the first element of a
    // construct a statement saying either the second element t < time or the expr holds for the second element
    // return new Or<Pair<T, BigInteger>>(new LessThan<T, Pair<T, BigInteger>>(
    // new Second<T, BigInteger, Pair<T, BigInteger>>(a), new ConstantExpr<T, Pair<T, BigInteger>>(time)));
    throw new NotImplementedException();
  }
}
