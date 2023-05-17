using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.Networks;
using Xunit;
using ZenLib;
using static ZenLib.Zen;
using Array = System.Array;

namespace Timepiece.Tests;

public static class DisagreeTests
{
  private static readonly Zen<BigInteger> _nullRoute = new BigInteger(20);

  private static AnnotatedNetwork<BigInteger, Unit> Net(
    Dictionary<string, Func<Zen<BigInteger>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Topologies.Complete(3);

    var initialValues = new Dictionary<string, Zen<BigInteger>>
    {
      {"A", new BigInteger(0)},
      {"B", _nullRoute},
      {"C", _nullRoute}
    };


    return new AnnotatedNetwork<BigInteger, Unit>(topology, topology.MapEdges(_ => Lang.Incr(1)), Merge, initialValues,
      annotations, topology.MapNodes(_ => Lang.Finally<BigInteger>(new BigInteger(2), ReachablePredicate)),
      topology.MapNodes<Func<Zen<BigInteger>, Zen<bool>>>(_ => ReachablePredicate),
      Array.Empty<SymbolicValue<Unit>>());
  }

  private static Zen<bool> ReachablePredicate(Zen<BigInteger> r)
  {
    return r < new BigInteger(3);
  }

  private static Zen<BigInteger> Merge(Zen<BigInteger> r1, Zen<BigInteger> r2)
  {
    // Prefer routes of length 0 < 2 < 1 < 3 < ...
    var zero = BigInteger.Zero;
    var two = new BigInteger(2);
    return If(r1 == zero, r1,
      If(r1 == two, r1,
        If(r2 == zero, r2,
          If(r2 == two, r2, Min(r1, r2)))));
  }

  [Fact]
  public static void SoundAnnotationsPassChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<BigInteger>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals<BigInteger>(BigInteger.Zero)},
      {
        "B", Lang.Until<BigInteger>(BigInteger.One, r => r == _nullRoute,
          r => And(r > BigInteger.Zero, r < new BigInteger(3)))
      },
      {
        "C", Lang.Until<BigInteger>(BigInteger.One, r => r == _nullRoute,
          r => And(r > BigInteger.Zero, r < new BigInteger(3)))
      }
    };

    var net = Net(annotations);

    NetworkAssert.CheckSound(net);
    // Assert.True(net.CheckAnnotations(), "Sound disagree annotations should pass checks.");
  }

  [Fact]
  public static void UnsoundAnnotationsFailChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<BigInteger>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals<BigInteger>(BigInteger.Zero)},
      {
        "B", Lang.Until<BigInteger>(BigInteger.One, r => r == _nullRoute,
          r => r == new BigInteger(1))
      },
      {
        "C", Lang.Until<BigInteger>(BigInteger.One, r => r == _nullRoute,
          r => r == new BigInteger(2))
      }
    };

    var net = Net(annotations);

    NetworkAssert.CheckUnsound(net);
    // Assert.False(net.CheckAnnotations(), "Unsound disagree annotations should fail checks.");
  }
}
