using System;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui;

using Time = Zen<BigInteger>;

public static class Lang
{
  /// <summary>
  ///     Generate an annotation: at and after time t, the predicate holds for a given route.
  /// </summary>
  /// <param name="t">An integer time.</param>
  /// <param name="after">A predicate over a route.</param>
  /// <typeparam name="T">The type of routes.</typeparam>
  /// <returns>A predicate over a route and time.</returns>
  public static Func<Zen<T>, Time, Zen<bool>> Finally<T>(Time t, Func<Zen<T>, Zen<bool>> after)
  {
    return (r, time) => Implies(time >= t, after(r));
  }

  /// <summary>
  ///     Generate an annotation: until time t, the predicate p1 holds for a given route.
  ///     At time t, the predicate p2 holds for the given route.
  /// </summary>
  /// <param name="t">An integer time.</param>
  /// <param name="before">A predicate over a route that applies before time t.</param>
  /// <param name="after">A predicate over a route that applies at and after time t.</param>
  /// <typeparam name="T">The type of routes.</typeparam>
  /// <returns>A predicate over a route and time.</returns>
  public static Func<Zen<T>, Time, Zen<bool>> Until<T>(Time t, Func<Zen<T>, Zen<bool>> before,
    Func<Zen<T>, Zen<bool>> after)
  {
    return (r, time) => If(time < t, before(r), after(r));
  }

  /// <summary>
  ///     Generate an annotation: for all time, the predicate holds for a given route.
  /// </summary>
  /// <param name="predicate">A predicate over a route.</param>
  /// <typeparam name="T">The type of routes.</typeparam>
  /// <returns>A predicate over a route and (ignored) time.</returns>
  public static Func<Zen<T>, Time, Zen<bool>> Globally<T>(Func<Zen<T>, Zen<bool>> predicate)
  {
    return (r, _) => predicate(r);
  }

  /// <summary>
  ///     Generate a globally annotation: for all time, the route is equal to the given parameter.
  /// </summary>
  /// <param name="route">The fixed route.</param>
  /// <typeparam name="T">The type of routes.</typeparam>
  /// <returns>A predicate over a route and (ignored) time.</returns>
  public static Func<Zen<T>, Time, Zen<bool>> Equals<T>(Zen<T> route)
  {
    return Globally<T>(r => r == route);
  }

  /// <summary>
  ///     Generate an annotation: for all time, the predicate *does not* hold for a given route.
  /// </summary>
  /// <param name="predicate">A predicate over a route.</param>
  /// <typeparam name="T">The type of routes.</typeparam>
  /// <returns>A predicate over a route and (ignored) time.</returns>
  public static Func<Zen<T>, Time, Zen<bool>> Never<T>(Func<Zen<T>, Zen<bool>> predicate)
  {
    return (r, _) => Not(predicate(r));
  }

  /// <summary>
  ///     Construct a function that ignores its argument and returns a constant route.
  /// </summary>
  /// <param name="val">The constant route to return.</param>
  /// <typeparam name="T">The type of routes.</typeparam>
  /// <returns>A function from routes to routes.</returns>
  public static Func<Zen<T>, Zen<T>> Const<T>(T val)
  {
    return _ => val;
  }

  /// <summary>
  ///     Construct an identity function over routes.
  /// </summary>
  /// <typeparam name="T">The type of routes.</typeparam>
  /// <returns>A function from routes to routes.</returns>
  public static Func<Zen<T>, Zen<T>> Identity<T>()
  {
    return r => r;
  }

  /// <summary>
  ///     Construct a function over tuples from functions over tuple elements.
  /// </summary>
  /// <param name="f1">A function from T1 to T3.</param>
  /// <param name="f2">A function from T2 to T4.</param>
  /// <typeparam name="T1"></typeparam>
  /// <typeparam name="T2"></typeparam>
  /// <typeparam name="T3"></typeparam>
  /// <typeparam name="T4"></typeparam>
  /// <returns>A function from T1*T2 to T3*T4.</returns>
  public static Func<Zen<Pair<T1, T2>>, Zen<Pair<T3, T4>>> Product<T1, T2, T3, T4>(Func<Zen<T1>, Zen<T3>> f1,
    Func<Zen<T2>, Zen<T4>> f2)
  {
    return prod => Pair(f1(prod.Item1()), f2(prod.Item2()));
  }

  public static Func<Zen<Pair<T1, T2>>, Zen<Pair<T3, T4>>, Zen<Pair<T5, T6>>> Product2<T1, T2, T3, T4, T5, T6>(
    Func<Zen<T1>, Zen<T3>, Zen<T5>> f1, Func<Zen<T2>, Zen<T4>, Zen<T6>> f2)
  {
    return (prod1, prod2) => Pair(f1(prod1.Item1(), prod2.Item1()), f2(prod1.Item2(), prod2.Item2()));
  }

  /// <summary>
  ///     Construct a function that tests a given route and delegates to one of two cases based on the result
  ///     of the test.
  /// </summary>
  /// <param name="test">A testing function from routes to bool.</param>
  /// <param name="trueCase">A function from routes to routes, executed when the test returns true.</param>
  /// <param name="falseCase">A function from routes to routes, executed when the test returns false.</param>
  /// <typeparam name="T">The type of routes.</typeparam>
  /// <returns>A function from routes to routes.</returns>
  public static Func<Zen<T>, Zen<T>> Test<T>(Func<Zen<T>, Zen<bool>> test, Func<Zen<T>, Zen<T>> trueCase,
    Func<Zen<T>, Zen<T>> falseCase)
  {
    return r => If(test(r), trueCase(r), falseCase(r));
  }

  public static Func<Zen<Option<T1>>, Zen<Option<T2>>> Omap<T1, T2>(Func<Zen<T1>, Zen<T2>> f)
  {
    return r => r.Select(f);
  }

  public static Func<Zen<Option<T1>>, Zen<Option<T2>>> Bind<T1, T2>(Func<Zen<T1>, Zen<Option<T2>>> f)
  {
    return r => r.Case(Null<T2>, f);
  }

  public static Func<Zen<Option<T>>, Zen<bool>> IsSome<T>()
  {
    return r => r.HasValue();
  }

  public static Func<Zen<Option<T>>, Zen<bool>> IsNone<T>()
  {
    return r => Not(r.HasValue());
  }

  /// <summary>
  ///     Construct a function that evaluates a predicate on a given optional route
  ///     and returns true if it has some value and that value satisfies the predicate,
  ///     otherwise false.
  /// </summary>
  /// <param name="f"></param>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
  public static Func<Zen<Option<T>>, Zen<bool>> IfSome<T>(Func<Zen<T>, Zen<bool>> f)
  {
    return r => And(r.HasValue(), f(r.Value()));
  }

  public static Func<Zen<Option<T>>, Zen<Option<T>>, Zen<Option<T>>> Omap2<T>(Func<Zen<T>, Zen<T>, Zen<T>> f)
  {
    return (r1, r2) => If(r1.HasValue(), If(r2.HasValue(), Some(f(r1.Value(), r2.Value())), r1), r2);
  }

  /// <summary>
  ///     Construct a function that increments a BigInteger by n.
  /// </summary>
  /// <param name="n">The amount to increment by.</param>
  /// <returns>A function from BigInteger to BigInteger.</returns>
  public static Func<Zen<BigInteger>, Zen<BigInteger>> Incr(BigInteger n)
  {
    return r => r + n;
  }

  /// <summary>
  ///     Construct a function that decrements a BigInteger by n.
  /// </summary>
  /// <param name="n">The amount to decrement by.</param>
  /// <returns>A function from BigInteger to BigInteger.</returns>
  public static Func<Zen<BigInteger>, Zen<BigInteger>> Decr(BigInteger n)
  {
    return r => r - n;
  }
}