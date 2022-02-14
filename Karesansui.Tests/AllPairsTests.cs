using System;
using System.Collections.Generic;
using System.Numerics;
using Karesansui.Networks;
using Xunit;
using ZenLib;
using static ZenLib.Zen;

namespace Karesansui.Tests;

public static class AllPairsTests
{
  private static AllPairs Net(
    Func<SymbolicValue<string>, Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>>
      annotations)
  {
    var topology = Default.Path(3);

    var convergeTime = new BigInteger(4);

    return new AllPairs(topology, annotations, convergeTime);
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
            Lang.Until(
              If(d.EqualsValue("A"), new BigInteger(0),
                If<BigInteger>(d.EqualsValue("B"), new BigInteger(1), new BigInteger(2))),
              Lang.IsNone<BigInteger>(), Lang.IsSome<BigInteger>())
          },
          {
            "B",
            Lang.Until(
              If<BigInteger>(d.DoesNotEqualValue("B"), new BigInteger(1), new BigInteger(0)),
              Lang.IsNone<BigInteger>(), Lang.IsSome<BigInteger>())
          },
          {
            "C",
            Lang.Until(
              If(d.EqualsValue("A"), new BigInteger(2),
                If<BigInteger>(d.EqualsValue("B"), new BigInteger(1), new BigInteger(0))),
              Lang.IsNone<BigInteger>(), Lang.IsSome<BigInteger>())
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
          {"A", Lang.Finally(new BigInteger(1), Lang.IsSome<BigInteger>())},
          {"B", Lang.Finally(new BigInteger(1), Lang.IsSome<BigInteger>())},
          {"C", Lang.Finally(new BigInteger(1), Lang.IsSome<BigInteger>())},
        });
    var net = Net(annotations);

    NetworkAssert.CheckUnsound(net);
  }
}
