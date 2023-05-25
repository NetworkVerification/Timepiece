using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.Networks;
using Timepiece.Tests.Networks;
using Xunit;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Tests;

using LpRoute = Pair<BigInteger, BigInteger>;

public static class LocalPrefTests
{
  private static LocalPref<string, Unit> Net
  {
    get
    {
      var topology = Topologies.Path(2);

      var initialValues = new Dictionary<string, Zen<LpRoute>>
      {
        {"A", Pair.Create(Constant(BigInteger.One), Constant(BigInteger.Zero))},
        {"B", Pair.Create(Constant(BigInteger.One), Constant(new BigInteger(10)))}
      };
      return new LocalPref<string, Unit>(topology, initialValues, System.Array.Empty<SymbolicValue<Unit>>());
    }
  }

  private static Zen<bool> IsReachable(Zen<LpRoute> r) => r.Item2() < new BigInteger(10);

  private static AnnotatedNetwork<LpRoute, string, Unit> AnnotatedNetwork(
    Dictionary<string, Func<Zen<LpRoute>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var convergeTime = new BigInteger(10);
    return new AnnotatedNetwork<LpRoute, string, Unit>(Net, annotations,
      Net.Topology.MapNodes<Func<Zen<LpRoute>, Zen<bool>>>(_ => IsReachable),
      Net.Topology.MapNodes(_ => Lang.True<LpRoute>()),
      convergeTime);
  }

  [Fact]
  public static void SoundAnnotationsPassChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<LpRoute>, Zen<BigInteger>, Zen<bool>>>
    {
      // NOTE: if we change the annotations from Item1() == 1 to Item1() <= 1,
      // the assertions will fail but the annotations still hold
      {
        "A",
        Lang.Equals<LpRoute>(Pair.Create<BigInteger, BigInteger>(BigInteger.One, BigInteger.Zero))
      },
      {
        "B",
        Lang.Until(BigInteger.One,
          r => r == Pair.Create<BigInteger, BigInteger>(BigInteger.One, new BigInteger(10)),
          Lang.Both<BigInteger, BigInteger>(fst => fst == BigInteger.One,
            snd => And(snd > BigInteger.Zero, snd < new BigInteger(10))))
      }
    };
    var net = AnnotatedNetwork(annotations);

    NetworkAssert.CheckSound(net);
  }

  [Fact]
  public static void UnsoundAnnotationsFailChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<LpRoute>, Zen<BigInteger>, Zen<bool>>>
    {
      {
        "A",
        Lang.Globally<LpRoute>(route => And(route.Item1() <= BigInteger.One,
          Implies(route.Item1() == BigInteger.One, route.Item2() == BigInteger.Zero)))
      },
      {
        "B",
        Lang.Until<LpRoute>(BigInteger.One, route => route.Item1() <= BigInteger.One,
          route => And(route.Item1() <= BigInteger.One,
            Implies(route.Item1() == BigInteger.One, route.Item2() < new BigInteger(10))))
      }
    };
    var net = AnnotatedNetwork(annotations);

    NetworkAssert.CheckUnsound(net);
  }
}
