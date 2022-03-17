using System.Numerics;
using Gardener.AstExpr;
using ZenLib;

namespace Gardener.AstFunction;

public class Finally<T> : AstPredicate<Pair<T, BigInteger>>
{
  public BigInteger Time { get; set; }

  public Finally(string arg, BigInteger time, Expr<bool, T> expr) : base(arg, Wrap(arg, expr, time))
  {
    Time = time;
  }

  private static Expr<bool, Pair<T, BigInteger>> Wrap(string arg, Expr<bool, T> expr, BigInteger time)
  {
    var a = new Var<Pair<T, BigInteger>>(arg);
    // construct a statement saying either the second element t < time or the expr holds for the second element
    // return new Or<Pair<T, BigInteger>>(new LessThan<T, Pair<T, BigInteger>>(
    // new Second<T, BigInteger, Pair<T, BigInteger>>(a), new ConstantExpr<T, Pair<T, BigInteger>>(time)));
    throw new NotImplementedException();
  }
}
