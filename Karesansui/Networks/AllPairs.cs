using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui.Networks;

public class AllPairs : ShortestPath<string>
{
    private static readonly SymbolicValue<string> d = new("dnode");

    public AllPairs(Topology topology,
      Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations,
                    BigInteger convergeTime) : base(topology,
                                                    topology.ForAllNodes(n => If(n.Equals(d.Name), Some<BigInteger>(BigInteger.Zero), Null<BigInteger>())),
                                                    annotations,
                                                    new[] { d }, convergeTime)
    {
        d.Constraint = DeriveDestConstraint(topology);
    }

    private static Func<Zen<string>, Zen<bool>> DeriveDestConstraint(Topology topology)
    {
        throw new NotImplementedException("todo");
    }

    private static AllPairs Net(
      Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations)
    {
        var topology = Default.Path(4);

        var convergeTime = new BigInteger(4);

        return new AllPairs(topology, annotations, convergeTime);
    }

    public static AllPairs Sound()
    {
        Console.WriteLine("Sound annotations:");
        // we cannot express these in terms of a single universal convergence time
        // instead, each key-value of the distmap has its own time which needs to be given
        var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
        {
            // {"A", (r, t) => And(r.Get("A").Value().HasValue(), IsSomeAfter(1)(r.Get("B").Value(), t), IsSomeAfter(2)(r.Get("C").Value(), t), IsSomeAfter(3)(r.Get("D").Value(), t))},
            // {"B", (r, t) => And(IsSomeAfter(1)(r.Get("A").Value(), t), r.Get("B").Value().HasValue(), IsSomeAfter(1)(r.Get("C").Value(), t), IsSomeAfter(2)(r.Get("D").Value(), t))},
            // {"C", (r, t) => And(IsSomeAfter(2)(r.Get("A").Value(), t), IsSomeAfter(1)(r.Get("B").Value(), t), r.Get("C").Value().HasValue(), IsSomeAfter(1)(r.Get("D").Value(), t))},
            // {"D", (r, t) => And(IsSomeAfter(3)(r.Get("A").Value(), t), IsSomeAfter(2)(r.Get("B").Value(), t), IsSomeAfter(1)(r.Get("C").Value(), t), r.Get("D").Value().HasValue())},
        };
        return Net(annotations);
    }

    public static AllPairs Unsound()
    {
        Console.WriteLine("Unsound annotations:");
        var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
        {
            // {
            //   "A",
            //   Lang.Finally<DistMap>(new BigInteger(10),
            //     r => r.All(pair => Lang.IfSome<BigInteger>(x => x < new BigInteger(2))(pair.Item2())))
            // }
            // {
            //   "B",
            //   Lang.Finally<DistMap>(new BigInteger(10),
            //     r => r.All(pair => Lang.IfSome<BigInteger>(x => x < new BigInteger(2))(pair.Item2())))
            // }
            // {
            //   "C",
            //   Lang.Finally<DistMap>(new BigInteger(10),
            //     r => r.All(pair => Lang.IfSome<BigInteger>(x => x < new BigInteger(2))(pair.Item2())))
            // }
            // {
            //   "D",
            //   Lang.Finally<DistMap>(new BigInteger(10),
            //     r => r.All(pair => Lang.IfSome<BigInteger>(x => x < new BigInteger(2))(pair.Item2())))
            // }
        };
        return Net(annotations);
    }

    private static Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>> IsSomeAfter(BigInteger t)
    {
        return Lang.Finally<Option<BigInteger>>(t, Lang.IsSome<BigInteger>());
    }
}
