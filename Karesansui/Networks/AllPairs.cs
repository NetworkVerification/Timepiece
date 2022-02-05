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
    BigInteger convergeTime) : base(topology, topology.ForAllEdges(_ => TransferFunction), MergeFunction,
    initialValues, annotations,
    topology.ForAllNodes(_ => Lang.Finally<DistMap>(convergeTime, ReachabilityAssertionStable)),
    topology.ForAllNodes(_ => ReachabilityAssertionStable), Array.Empty<SymbolicValue<Unit>>())
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

    var convergeTime = new BigInteger(10);

    return new AllPairs(topology, initialValues, annotations, convergeTime);
  }

  public static Network<DistMap, Unit> Sound()
  {
    Console.WriteLine("Sound annotations:");
    // FIXME: we cannot express these in terms of a single universal convergence time
    // instead, each key-value of the distmap has its own time which needs to be given
    var annotations = new Dictionary<string, Func<Zen<DistMap>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Finally<DistMap>(new BigInteger(10), ReachabilityAssertionStable)},
      {"B", Lang.Finally<DistMap>(new BigInteger(10), ReachabilityAssertionStable)},
      {"C", Lang.Finally<DistMap>(new BigInteger(10), ReachabilityAssertionStable)},
      {"D", Lang.Finally<DistMap>(new BigInteger(10), ReachabilityAssertionStable)}
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


  private static Zen<bool> ReachabilityAssertionStable(Zen<DistMap> route)
  {
    return route.All(pair => Lang.IsSome<BigInteger>()(pair.Item2()));
  }

  private static Zen<DistMap> MergeFunction(Zen<DistMap> r1, Zen<DistMap> r2)
  {
    return r1.Zip(r2, Lang.Omap2<BigInteger>(Min));
  }

  private static Zen<DistMap> TransferFunction(Zen<DistMap> route)
  {
    return route.ForEach(Lang.Omap(Lang.Incr(1)));
  }
}