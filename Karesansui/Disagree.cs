using System;
using System.Collections.Generic;
using System.Numerics;
using Karesansui.Networks;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui;

public static class Disagree
{
  public static Network<BigInteger, Unit> Net(
    Dictionary<string, Func<Zen<BigInteger>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Default.Complete(3);

    var initialValues = new Dictionary<string, Zen<BigInteger>>
    {
      {"A", new BigInteger(0)},
      {"B", new BigInteger(20)},
      {"C", new BigInteger(20)}
    };


    return new Network<BigInteger, Unit>(topology, topology.ForAllEdges(_ => Lang.Incr(1)), Merge, initialValues,
      annotations, topology.ForAllNodes(_ => Lang.Finally<BigInteger>(new BigInteger(2), ReachablePredicate)),
      topology.ForAllNodes(_ => ReachablePredicate), Array.Empty<SymbolicValue<Unit>>());
  }

  public static Network<BigInteger, Unit> Sound()
  {
    Console.WriteLine("Sound annotations:");

    var annotations = new Dictionary<string, Func<Zen<BigInteger>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals<BigInteger>(new BigInteger(0))},
      {
        "B", Lang.Until<BigInteger>(new BigInteger(1), r => r == new BigInteger(20),
          r => And(r > new BigInteger(0), r < new BigInteger(3)))
      },
      {
        "C", Lang.Until<BigInteger>(new BigInteger(1), r => r == new BigInteger(20),
          r => And(r > new BigInteger(0), r < new BigInteger(3)))
      }
    };

    return Net(annotations);
  }

  public static Network<BigInteger, Unit> AlternateSound()
  {
    Console.WriteLine("Sound annotations:");

    var annotations = new Dictionary<string, Func<Zen<BigInteger>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals<BigInteger>(new BigInteger(0))}
      // TODO: add annotations for B and C using evenness/oddness to choose the routes
    };

    return Net(annotations);
  }

  private static Zen<bool> ReachablePredicate(Zen<BigInteger> r)
  {
    return r < new BigInteger(3);
  }

  private static Zen<BigInteger> Merge(Zen<BigInteger> r1, Zen<BigInteger> r2)
  {
    // Prefer routes of length 0 < 2 < 1 < 3 < ...
    var zero = new BigInteger(0);
    var two = new BigInteger(2);
    return If(r1 == zero, r1,
      If(r1 == two, r1,
        If(r2 == zero, r2,
          If(r2 == two, r2, Min(r1, r2)))));
  }
}