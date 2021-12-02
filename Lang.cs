using System;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace ZenDemo;

using Time = Zen<BigInteger>;

public static class Lang
{
    /// <summary>
    ///     Generate an annotation: after time t, the predicate holds for a given route.
    /// </summary>
    /// <param name="t">An integer time.</param>
    /// <param name="predicate">A predicate over a route.</param>
    /// <typeparam name="T">The type of routes.</typeparam>
    /// <returns>A predicate over a route and time.</returns>
    public static Func<Zen<T>, Time, Zen<bool>> After<T>(Time t, Func<Zen<T>, Zen<bool>> predicate)
    {
        return (r, time) => Implies(time > t, predicate(r));
    }

    /// <summary>
    ///     Generate an annotation: for all time, the predicate holds for a given route.
    /// </summary>
    /// <param name="predicate">A predicate over a route.</param>
    /// <typeparam name="T">The type of routes.</typeparam>
    /// <returns>A predicate over a route and (ignored) time.</returns>
    public static Func<Zen<T>, Time, Zen<bool>> Always<T>(Func<Zen<T>, Zen<bool>> predicate)
    {
        return (r, _) => predicate(r);
    }

    /// <summary>
    ///     Generate an always annotation: for all time, the route is equal to the given parameter.
    /// </summary>
    /// <param name="route">The fixed route.</param>
    /// <typeparam name="T">The type of routes.</typeparam>
    /// <returns>A predicate over a route and (ignored) time.</returns>
    public static Func<Zen<T>, Time, Zen<bool>> Equals<T>(Zen<T> route)
    {
        return Always<T>(r => r == route);
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
    /// <param name="f1"></param>
    /// <param name="f2"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <returns></returns>
    public static Func<Zen<Pair<T1, T2>>, Zen<Pair<T1, T2>>> Product<T1, T2>(Func<Zen<T1>, Zen<T1>> f1,
        Func<Zen<T2>, Zen<T2>> f2)
    {
        return prod => Pair(f1(prod.Item1()), f2(prod.Item2()));
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

    public static Func<Zen<Option<T>>, Zen<Option<T>>> Omap<T>(Func<Zen<T>, Zen<T>> f)
    {
        return r => r.Select(f);
    }

    public static Func<Zen<Option<T>>, Zen<bool>> IsSome<T>()
    {
        return r => r.HasValue();
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
    ///     Construct a function that increments a uint by n.
    /// </summary>
    /// <param name="n">The amount to increment by.</param>
    /// <returns>A function from uint to uint.</returns>
    public static Func<Zen<uint>, Zen<uint>> Incr(uint n)
    {
        return r => r + n;
    }

    /// <summary>
    ///     Construct a function that decrements a uint by n.
    /// </summary>
    /// <param name="n">The amount to decrement by.</param>
    /// <returns>A function from uint to uint.</returns>
    public static Func<Zen<uint>, Zen<uint>> Decr(uint n)
    {
        return r => r - n;
    }
}