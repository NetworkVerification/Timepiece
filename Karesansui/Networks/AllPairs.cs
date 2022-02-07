using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui.Networks;

using DistMap = Dict<string, Option<BigInteger>>;

public class AllPairs : Network<Dict<string, Option<BigInteger>>, Unit>
{
    public AllPairs(Topology topology,
      Dictionary<string, Zen<DistMap>> initialValues,
      Dictionary<string, Func<Zen<DistMap>, Zen<BigInteger>, Zen<bool>>> annotations,
                    BigInteger convergeTime) : base(topology,
                                                    topology.ForAllEdges<Func<Zen<DistMap>, Zen<DistMap>>>(_ => TransferFunction),
                                                    MergeFunction(topology),
      initialValues, annotations,
      topology.ForAllNodes(_ => Lang.Finally<DistMap>(convergeTime, ReachabilityAssertionStable(topology))),
      topology.ForAllNodes<Func<Zen<DistMap>, Zen<bool>>>(_ => ReachabilityAssertionStable(topology)), Array.Empty<SymbolicValue<Unit>>())
    {
    }

    public static Network<DistMap, Unit> Net(
      Dictionary<string, Func<Zen<DistMap>, Zen<BigInteger>, Zen<bool>>> annotations)
    {
        var topology = Default.Path(4);

        var initialValues =
          topology.ForAllNodes(node =>
            Constant(topology.ToNodeDict(n =>
              node.Equals(n) ? Option.Some(new BigInteger(0)) : Option.None<BigInteger>())));

        var convergeTime = new BigInteger(4);

        return new AllPairs(topology, initialValues, annotations, convergeTime);
    }

    public static Network<DistMap, Unit> Sound()
    {
        Console.WriteLine("Sound annotations:");
        // we cannot express these in terms of a single universal convergence time
        // instead, each key-value of the distmap has its own time which needs to be given
        var annotations = new Dictionary<string, Func<Zen<DistMap>, Zen<BigInteger>, Zen<bool>>>
    {
      // {"A", Lang.Until<DistMap>(new BigInteger(10), route => route.Get("A").HasValue(), ReachabilityAssertionStable)},
      {"A", (r, t) => And(r.Get("A").Value().HasValue(), IsSomeAfter(1)(r.Get("B").Value(), t), IsSomeAfter(2)(r.Get("C").Value(), t), IsSomeAfter(3)(r.Get("D").Value(), t))},
      // {"B", Lang.Until<DistMap>(new BigInteger(10), route => route.Get("B").HasValue(), ReachabilityAssertionStable)},
      {"B", (r, t) => And(IsSomeAfter(1)(r.Get("A").Value(), t), r.Get("B").Value().HasValue(), IsSomeAfter(1)(r.Get("C").Value(), t), IsSomeAfter(2)(r.Get("D").Value(), t))},
      // {"C", Lang.Until<DistMap>(new BigInteger(10), route => route.Get("C").HasValue(), ReachabilityAssertionStable)},
      {"C", (r, t) => And(IsSomeAfter(2)(r.Get("A").Value(), t), IsSomeAfter(1)(r.Get("B").Value(), t), r.Get("C").Value().HasValue(), IsSomeAfter(1)(r.Get("D").Value(), t))},
      // {"D", Lang.Until<DistMap>(new BigInteger(10), route => route.Get("D").HasValue(), ReachabilityAssertionStable)}
      {"D", (r, t) => And(IsSomeAfter(3)(r.Get("A").Value(), t), IsSomeAfter(2)(r.Get("B").Value(), t), IsSomeAfter(1)(r.Get("C").Value(), t), r.Get("D").Value().HasValue())},
    };
        return Net(annotations);
    }

    public static Network<DistMap, Unit> Unsound()
    {
        Console.WriteLine("Unsound annotations:");
        var annotations = new Dictionary<string, Func<Zen<DistMap>, Zen<BigInteger>, Zen<bool>>>
    {
      {
        "A",
        Lang.Finally<DistMap>(new BigInteger(10),
          r => r.All(pair => Lang.IfSome<BigInteger>(x => x < new BigInteger(2))(pair.Item2())))
      },
      {
        "B",
        Lang.Finally<DistMap>(new BigInteger(10),
          r => r.All(pair => Lang.IfSome<BigInteger>(x => x < new BigInteger(2))(pair.Item2())))
      },
      {
        "C",
        Lang.Finally<DistMap>(new BigInteger(10),
          r => r.All(pair => Lang.IfSome<BigInteger>(x => x < new BigInteger(2))(pair.Item2())))
      },
      {
        "D",
        Lang.Finally<DistMap>(new BigInteger(10),
          r => r.All(pair => Lang.IfSome<BigInteger>(x => x < new BigInteger(2))(pair.Item2())))
      }
    };
        return Net(annotations);
    }

    private static Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>> IsSomeAfter(BigInteger t)
    {
        return Lang.Finally<Option<BigInteger>>(t, Lang.IsSome<BigInteger>());
    }

    private static Func<Zen<DistMap>, Zen<bool>> ReachabilityAssertionStable(Topology topology)
    {
        return route => topology.FoldNodes(Constant(true), (b, node) => And(b, route.Get(node).Value().HasValue()));
    }

    private static Func<Zen<DistMap>, Zen<DistMap>, Zen<DistMap>> MergeFunction(Topology topology)
    {
        return (r1, r2) => Constant(topology.ToNodeDict(n => Lang.Omap2<BigInteger>(Min)(r1.Get(n).Value(), r2.Get(n).Value())));
    }

    private static Zen<DistMap> TransferFunction(Zen<DistMap> route)
    {
        return route.ForEach(Lang.Omap(Lang.Incr(1)));
    }
}
