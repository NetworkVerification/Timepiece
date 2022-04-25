using System;
using System.Collections.Generic;
using System.Numerics;
using Timekeeper.Networks;
using Xunit;
using ZenLib;
using static ZenLib.Zen;

namespace Timekeeper.Tests;

public static class AllPairsTests
{
  private static AllPairs Net(
    Func<SymbolicValue<string>, Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>>
      annotations)
  {
    var topology = Topologies.Path(3);

    var convergeTime = new BigInteger(4);

    var safetyProperties = topology.ForAllNodes(_ => Lang.IsSome<BigInteger>());
    return new AllPairs(topology, annotations, convergeTime, safetyProperties);
  }

  [Fact]
  public static void SoundAnnotationsPassChecks()
  {
    var annotations =
      new Func<SymbolicValue<string>, Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>>(
        d => new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
        {
          {
            "A",
            Lang.Until<Option<BigInteger>>(
              If(d.EqualsValue("A"), new BigInteger(0),
                If<BigInteger>(d.EqualsValue("B"), new BigInteger(1), new BigInteger(2))),
              Option.IsNone, Option.IsSome)
          },
          {
            "B",
            Lang.Until<Option<BigInteger>>(
              If<BigInteger>(d.DoesNotEqualValue("B"), new BigInteger(1), new BigInteger(0)),
              Option.IsNone, Option.IsSome)
          },
          {
            "C",
            Lang.Until<Option<BigInteger>>(
              If(d.EqualsValue("A"), new BigInteger(2),
                If<BigInteger>(d.EqualsValue("B"), new BigInteger(1), new BigInteger(0))),
              Option.IsNone, Option.IsSome)
          }
        });
    var net = Net(annotations);

    NetworkAssert.CheckSound(net);
  }

  [Fact]
  public static void UnsoundAnnotationsFailChecks()
  {
    var annotations =
      new Func<SymbolicValue<string>, Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>>(
        _ => new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
        {
          {"A", Lang.Finally<Option<BigInteger>>(new BigInteger(1), Option.IsSome)},
          {"B", Lang.Finally<Option<BigInteger>>(new BigInteger(1), Option.IsSome)},
          {"C", Lang.Finally<Option<BigInteger>>(new BigInteger(1), Option.IsSome)},
        });
    var net = Net(annotations);

    NetworkAssert.CheckUnsoundCheck(net, SmtCheck.Inductive);
  }
}
