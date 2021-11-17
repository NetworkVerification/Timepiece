using System;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace ZenDemo
{
    using Time = Zen<BigInteger>;

    public static class Lang
    {
        /// <summary>
        /// Generate an annotation: after time t, the predicate holds for a given route.
        /// </summary>
        /// <param name="t">An integer time.</param>
        /// <param name="predicate">A predicate over a route.</param>
        /// <typeparam name="T">The type of routes.</typeparam>
        /// <returns>Return a predicate over a route and time.</returns>
        public static Func<Zen<T>, Time, Zen<bool>> After<T>(Time t, Func<Zen<T>, Zen<bool>> predicate)
        {
            return (r, time) => Implies(time > t, predicate(r));
        }
    }
}