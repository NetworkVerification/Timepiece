using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece;

using Time = Zen<BigInteger>;

/// <summary>
///   Functions for describing predicates over routes and temporal invariants over routes.
/// </summary>
public static class Lang
{
  /// <summary>
  ///   Generate an annotation: at and after time t, the predicate holds for a given route.
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
  ///   Generate an annotation: until time t, the predicate p1 holds for a given route.
  ///   At time t, the predicate p2 holds for the given route.
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
  ///   Generate an annotation: for all time, the predicate holds for a given route.
  /// </summary>
  /// <param name="predicate">A predicate over a route.</param>
  /// <typeparam name="T">The type of routes.</typeparam>
  /// <returns>A predicate over a route and (ignored) time.</returns>
  public static Func<Zen<T>, Time, Zen<bool>> Globally<T>(Func<Zen<T>, Zen<bool>> predicate)
  {
    return (r, _) => predicate(r);
  }

  /// <summary>
  ///   Generate a globally annotation: for all time, the route is equal to the given parameter.
  /// </summary>
  /// <param name="route">The fixed route.</param>
  /// <typeparam name="T">The type of routes.</typeparam>
  /// <returns>A predicate over a route and (ignored) time.</returns>
  public static Func<Zen<T>, Time, Zen<bool>> Equals<T>(Zen<T> route)
  {
    return Globally<T>(r => r == route);
  }

  /// <summary>
  ///   Generate an annotation: for all time, the predicate *does not* hold for a given route.
  /// </summary>
  /// <param name="predicate">A predicate over a route.</param>
  /// <typeparam name="T">The type of routes.</typeparam>
  /// <returns>A predicate over a route and (ignored) time.</returns>
  public static Func<Zen<T>, Time, Zen<bool>> Never<T>(Func<Zen<T>, Zen<bool>> predicate)
  {
    return Globally(Not(predicate));
  }

  /// <summary>
  ///   Compose two functions.
  /// </summary>
  /// <param name="f1">The first function.</param>
  /// <param name="f2">The second function.</param>
  /// <typeparam name="T">The type of their arguments.</typeparam>
  /// <returns>A composition of both functions (call f1, then call f2 on f1's output).</returns>
  public static Func<T, T> Compose<T>(Func<T, T> f1, Func<T, T> f2)
  {
    return t => f2(f1(t));
  }

  /// <summary>
  ///   Compose an arbitrary number of functions.
  /// </summary>
  /// <param name="fs">The sequence of functions to compose.</param>
  /// <typeparam name="T">The type of their arguments.</typeparam>
  /// <returns>A composition of the functions in order.</returns>
  public static Func<T, T> Compose<T>(params Func<T, T>[] fs)
  {
    return fs.Aggregate(Compose);
  }

  /// <summary>
  ///   Construct a function that ignores its argument and returns a constant route.
  /// </summary>
  /// <param name="val">The constant route to return.</param>
  /// <typeparam name="T">The type of routes.</typeparam>
  /// <returns>A function from routes to routes.</returns>
  public static Func<Zen<T>, Zen<T>> Const<T>(T val)
  {
    return _ => val;
  }

  /// <summary>
  ///   Construct an identity function over routes.
  /// </summary>
  /// <typeparam name="T">The type of routes.</typeparam>
  /// <returns>A function from routes to routes.</returns>
  public static Func<Zen<T>, Zen<T>> Identity<T>() => r => r;

  public static Func<Zen<T>, Zen<bool>> True<T>() => _ => Zen.True();

  public static Func<Zen<T>, Zen<bool>> False<T>() => _ => Zen.False();

  /// <summary>
  ///   Construct a function over tuples from functions over tuple elements.
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
    return prod => Pair.Create(f1(prod.Item1()), f2(prod.Item2()));
  }

  public static Func<Zen<Pair<T1, T2>>, Zen<Pair<T3, T4>>, Zen<Pair<T5, T6>>> Product2<T1, T2, T3, T4, T5, T6>(
    Func<Zen<T1>, Zen<T3>, Zen<T5>> f1, Func<Zen<T2>, Zen<T4>, Zen<T6>> f2)
  {
    return (prod1, prod2) => Pair.Create(f1(prod1.Item1(), prod2.Item1()), f2(prod1.Item2(), prod2.Item2()));
  }

  /// <summary>
  ///   Return a predicate over a pair where f1 holds on the first element and f2 holds on the second element.
  /// </summary>
  /// <param name="f1"></param>
  /// <param name="f2"></param>
  /// <typeparam name="T1"></typeparam>
  /// <typeparam name="T2"></typeparam>
  /// <returns></returns>
  public static Func<Zen<Pair<T1, T2>>, Zen<bool>> Both<T1, T2>(Func<Zen<T1>, Zen<bool>> f1,
    Func<Zen<T2>, Zen<bool>> f2)
  {
    return prod => And(f1(prod.Item1()), f2(prod.Item2()));
  }

  public static Func<Zen<Pair<T1, T2>>, Zen<bool>> First<T1, T2>(Func<Zen<T1>, Zen<bool>> f)
  {
    return prod => f(prod.Item1());
  }

  public static Func<Zen<Pair<T1, T2>>, Zen<bool>> Second<T1, T2>(Func<Zen<T2>, Zen<bool>> f)
  {
    return prod => f(prod.Item2());
  }

  /// <summary>
  ///   Return a merge function lifted from the given merge function and an accessor function
  ///   to convert the new arguments to the original function's arguments.
  /// </summary>
  /// <param name="f">A merge function over T2.</param>
  /// <param name="by">A function mapping T1 to T2.</param>
  /// <typeparam name="T1">The new merge function's type.</typeparam>
  /// <typeparam name="T2">The original merge function's type.</typeparam>
  /// <returns>A merge function over T1.</returns>
  public static Func<Zen<T1>, Zen<T1>, Zen<T1>> MergeBy<T1, T2>(Func<Zen<T2>, Zen<T2>, Zen<T2>> f,
    Func<Zen<T1>, Zen<T2>> by)
  {
    return (t1, t2) =>
    {
      var b1 = by(t1);
      var b2 = by(t2);
      return If(f(b1, b2) == b1, t1, t2);
    };
  }

  /// <summary>
  ///   Construct a function that tests a given route and delegates to one of two cases based on the result of the test.
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
    return r => r.Case(Option.Null<T2>, f);
  }

  public static Func<Zen<Option<T>>, Zen<bool>> IsSome<T>()
  {
    return r => r.IsSome();
  }

  public static Func<Zen<Option<T>>, Zen<bool>> IsNone<T>()
  {
    return r => r.IsNone();
  }

  /// <summary>
  ///   Construct a function that evaluates a predicate on a given optional route
  ///   and returns true if it has some value and that value satisfies the predicate,
  ///   otherwise false.
  /// </summary>
  /// <param name="f"></param>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
  public static Func<Zen<Option<T>>, Zen<bool>> IfSome<T>(Func<Zen<T>, Zen<bool>> f)
  {
    return r => r.Where(f).IsSome();
  }

  /// <summary>
  ///   Construct a function that evaluates a predicate on a given optional route
  ///   and returns true if it either has some value and that value satisfies the predicate,
  ///   or if it is None.
  /// </summary>
  /// <param name="f"></param>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
  public static Func<Zen<Option<T>>, Zen<bool>> OrSome<T>(Func<Zen<T>, Zen<bool>> f)
  {
    return r => r.Where(Not(f)).IsNone();
  }

  public static Func<Zen<Option<T>>, Zen<Option<T>>, Zen<Option<T>>> Omap2<T>(Func<Zen<T>, Zen<T>, Zen<T>> f)
  {
    return (r1, r2) => If(r1.IsSome(),
      If(r2.IsSome(), Option.Create(f(r1.Value(), r2.Value())), r1), r2);
  }

  /// <summary>
  ///   Construct a function that increments a BigInteger by n.
  /// </summary>
  /// <param name="n">The amount to increment by.</param>
  /// <returns>A function from BigInteger to BigInteger.</returns>
  public static Func<Zen<BigInteger>, Zen<BigInteger>> Incr(BigInteger n)
  {
    return r => r + n;
  }

  /// <summary>
  ///   Return the intersection of the given predicates,
  ///   i.e. return a predicate that returns true if all the fs hold for a given value.
  /// </summary>
  /// <param name="fs">A variable-length array of predicate functions.</param>
  /// <typeparam name="T">The type of the predicate arguments.</typeparam>
  /// <returns>True if all the predicates hold, false otherwise.</returns>
  public static Func<Zen<T>, Zen<bool>> Intersect<T>(params Func<Zen<T>, Zen<bool>>[] fs)
  {
    return r => And(fs.Select(f => f(r)).ToArray());
  }

  /// <summary>
  ///   Return the intersection of the given predicates,
  ///   i.e. return a predicate that returns true if all the fs hold for a given value.
  /// </summary>
  /// <param name="fs">A variable-length array of predicate functions.</param>
  /// <typeparam name="T">The type of the predicate arguments.</typeparam>
  /// <returns>True if all the predicates hold, false otherwise.</returns>
  public static Func<Zen<T>, Time, Zen<bool>> Intersect<T>(
    params Func<Zen<T>, Time, Zen<bool>>[] fs)
  {
    return (r, t) => And(fs.Select(f => f(r, t)).ToArray());
  }

  public static Func<Zen<T>, Zen<bool>> Union<T>(params Func<Zen<T>, Zen<bool>>[] fs)
  {
    return r => Or(fs.Select(f => f(r)).ToArray());
  }

  /// <summary>
  ///   Return the complement of the given predicate <paramref name="f"/>,
  ///   i.e. the one that returns true for all inputs for which <paramref name="f"/> returns false.
  /// </summary>
  /// <param name="f"></param>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
  public static Func<Zen<T>, Zen<bool>> Not<T>(Func<Zen<T>, Zen<bool>> f)
  {
    return r => Zen.Not(f(r));
  }

  /// <summary>
  ///   Return a function comparing two objects of type T using the specified key accessor and the specified key comparator.
  ///   If the comparator returns true given keyAccessor(object1) and keyAccessor(object2), object1 is returned.
  ///   If the comparator returns false given keyAccessor(object1) and keyAccessor(object2),
  ///   it tests object2 and object1 (in reverse order).
  ///   If the second comparison returns true, object2 is returned.
  ///   If the second comparison returns false, the fallthrough is executed on the two objects.
  ///   (A natural fallthrough in this case would be to return the second object, thereby replicating an If.
  ///   This method's benefit comes from allowing us to chain comparisons in sequence.)
  /// </summary>
  /// <param name="keyAccessor">The function used to access the objects' keys.</param>
  /// <param name="keyComparator">The function used to compare the keys.</param>
  /// <param name="fallThrough">The function to call if the keyComparator returns false.</param>
  /// <typeparam name="T">The type of objects.</typeparam>
  /// <typeparam name="TKey">The type of keys.</typeparam>
  /// <returns>
  ///   A function returning the first object if the comparator evaluates to true,
  ///   returning the second if the comparator evaluates to true given the objects in reverse order,
  ///   and otherwise calling the fallthrough.
  /// </returns>
  public static Func<Zen<T>, Zen<T>, Zen<T>> CompareBy<T, TKey>(
    Func<Zen<T>, Zen<TKey>> keyAccessor,
    Func<Zen<TKey>, Zen<TKey>, Zen<bool>> keyComparator,
    Func<Zen<T>, Zen<T>, Zen<T>> fallThrough)
  {
    return (t1, t2) => If(keyComparator(keyAccessor(t1), keyAccessor(t2)), t1,
      If(keyComparator(keyAccessor(t2), keyAccessor(t1)), t2, fallThrough(t1, t2)));
  }

  /// <summary>
  ///   Return a function comparing two objects of type T using the specified key accessor and the specified key comparator.
  ///   If the comparator returns true given keyAccessor(object1) and keyAccessor(object2), object1 is returned;
  ///   otherwise, object2 is returned.
  ///   To use a comparison that tests both directions with a fall-through case, see the version of this function
  ///   with an additional fallThrough parameter.
  /// </summary>
  /// <param name="keyAccessor">The function used to access the objects' keys.</param>
  /// <param name="keyComparator">The function used to compare the keys.</param>
  /// <typeparam name="T">The type of objects.</typeparam>
  /// <typeparam name="TKey">The type of keys.</typeparam>
  /// <returns>
  ///   A function returning the first object if the comparator evaluates to true, and otherwise returning the second.
  /// </returns>
  public static Func<Zen<T>, Zen<T>, Zen<T>> CompareBy<T, TKey>(
    Func<Zen<T>, Zen<TKey>> keyAccessor,
    Func<Zen<TKey>, Zen<TKey>, Zen<bool>> keyComparator)
  {
    return (t1, t2) => If(keyComparator(keyAccessor(t1), keyAccessor(t2)), t1, t2);
  }

  public static Zen<bool> Exists<T>(this IEnumerable<T> enumerable, Func<T, Zen<bool>> predicate) =>
    enumerable.Aggregate(Zen.False(), (b, e) => Or(b, predicate(e)));

  public static Zen<bool> ForAll<T>(this IEnumerable<T> enumerable, Func<T, Zen<bool>> predicate) =>
    enumerable.Aggregate(Zen.True(), (b, e) => And(b, predicate(e)));
}
