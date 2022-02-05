using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui.Networks;

public class LocalPref : Network<Pair<BigInteger, BigInteger>, Unit>
{
    public LocalPref(Topology topology,
        Dictionary<string, Zen<Pair<BigInteger, BigInteger>>> initialValues,
        Dictionary<string, Func<Zen<Pair<BigInteger, BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations,
        BigInteger convergeTime)
        : base(topology,
            topology.ForAllEdges(_ => Lang.Product(Lang.Identity<BigInteger>(), Lang.Incr(new BigInteger(1)))),
            Merge,
            initialValues,
            annotations,
            topology.ForAllNodes(_ => Lang.Finally<Pair<BigInteger, BigInteger>>(convergeTime, ReachabilityProperty)),
            topology.ForAllNodes(_ => ReachabilityProperty), new Dictionary<Zen<Unit>, Func<Zen<Unit>, Zen<bool>>>())
    {
    }

    /// <summary>
    ///     Generate a simple example network.
    /// </summary>
    public static Network<Pair<BigInteger, BigInteger>, Unit> Net(
        Dictionary<string, Func<Zen<Pair<BigInteger, BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations)
    {
        var topology = Default.Path(2);

        var initialValues = new Dictionary<string, Zen<Pair<BigInteger, BigInteger>>>
        {
            {"A", Pair(Constant(new BigInteger(1)), Constant(new BigInteger(0)))},
            {"B", Pair(Constant(new BigInteger(1)), Constant(new BigInteger(10)))}
        };

        var convergeTime = new BigInteger(10);
        return new LocalPref(topology, initialValues, annotations, convergeTime);
    }

    public static Network<Pair<BigInteger, BigInteger>, Unit> Sound()
    {
        Console.WriteLine("Sound annotations:");
        var annotations = new Dictionary<string, Func<Zen<Pair<BigInteger, BigInteger>>, Zen<BigInteger>, Zen<bool>>>
        {
            // NOTE: if we change the annotations from Item1() == 1 to Item1() <= 1,
            // the assertions will fail but the annotations still hold
            {
                "A",
                (route, _) => And(route.Item1() == new BigInteger(1),
                    Implies(route.Item1() == new BigInteger(1), route.Item2() == new BigInteger(0)))
            },
            {
                "B",
                (route, time) => And(route.Item1() == new BigInteger(1),
                    Implies(And(route.Item1() == new BigInteger(1), time > new BigInteger(0)),
                        route.Item2() < new BigInteger(10)))
            }
        };
        return Net(annotations);
    }

    public static Network<Pair<BigInteger, BigInteger>, Unit> Unsound()
    {
        Console.WriteLine("Unsound annotations:");
        var annotations = new Dictionary<string, Func<Zen<Pair<BigInteger, BigInteger>>, Zen<BigInteger>, Zen<bool>>>
        {
            {
                "A",
                (route, _) => And(route.Item1() <= new BigInteger(1),
                    Implies(route.Item1() == new BigInteger(1), route.Item2() == new BigInteger(0)))
            },
            {
                "B",
                (route, time) => And(route.Item1() <= new BigInteger(1),
                    Implies(And(route.Item1() == new BigInteger(1), time > new BigInteger(0)),
                        route.Item2() < new BigInteger(10)))
            }
        };
        return Net(annotations);
    }

    /// <summary>
    ///     The merge function for the simple path length network.
    /// </summary>
    private static Zen<Pair<BigInteger, BigInteger>> Merge(Zen<Pair<BigInteger, BigInteger>> r1,
        Zen<Pair<BigInteger, BigInteger>> r2)
    {
        var (r1First, r1Second) = (r1.Item1(), r1.Item2());
        var (r2First, r2Second) = (r2.Item1(), r2.Item2());
        var cmp = If(r1Second < r2Second, r1, r2);
        return If(r1First < r2First, r1, If(r1First == r2First, cmp, r2));
    }

    /// <summary>
    ///     Final assertion we want to check for the stable paths encoding that removes time.
    /// </summary>
    private static Zen<bool> ReachabilityProperty(Zen<Pair<BigInteger, BigInteger>> r)
    {
        return r.Item2() < new BigInteger(10);
    }
}