using System;
using System.Collections.Generic;
using System.Numerics;
using Karesansui.Datatypes;
using Karesansui.Networks;
using Xunit;
using ZenLib;
using static ZenLib.Zen;

namespace Karesansui.Tests;

public static class BgpNetworkTests
{
  private static readonly Zen<Option<Bgp>> Start = Option.Create<Bgp>(new Bgp(100, 0, new FBag<string>()));

  private static BgpNetwork Net(Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Topologies.Path(3);

    var initialValues = new Dictionary<string, Zen<Option<Bgp>>>
    {
      {"A", Start},
      {"B", Option.None<Bgp>()},
      {"C", Option.None<Bgp>()}
    };

    var convergeTime = new BigInteger(3);

    return new BgpNetwork(topology, initialValues, annotations, convergeTime, Array.Empty<SymbolicValue<Bgp>>());
  }

  [Fact]
  public static void SoundAnnotationsPassChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals(Start)},
      {
        "B",
        Lang.Until(new BigInteger(1), Lang.IsNone<Bgp>(),
          Lang.IfSome<Bgp>(b =>
            And(b.GetAsLength() == new BigInteger(1), b.GetLp() == new BigInteger(100), Not(b.HasTag("C")))))
      },
      {
        "C",
        Lang.Until(new BigInteger(2), Lang.IsNone<Bgp>(),
          Lang.IfSome<Bgp>(b =>
            And(b.GetAsLength() == new BigInteger(1), b.GetLp() == new BigInteger(100), b.HasTag("B"))))
      }
    };

    var net = Net(annotations);

    NetworkAssert.CheckSound(net);
  }

  [Fact]
  public static void ExactAnnotationsPassChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {
        "A",
        Lang.Equals(Start)
      },
      {
        "B",
        Lang.Until(new BigInteger(1), Lang.IsNone<Bgp>(),
          Lang.IfSome<Bgp>(b =>
            And(b.GetAsLength() == new BigInteger(1), b.GetLp() == new BigInteger(100),
              b.GetTags() == FBag.Create(Constant("A")))))
      },
      {
        "C",
        Lang.Until(new BigInteger(2), Lang.IsNone<Bgp>(),
          Lang.IfSome<Bgp>(b =>
            And(b.GetAsLength() == new BigInteger(2), b.GetLp() == new BigInteger(100),
              b.GetTags() == FBag.Create(Constant("A"), Constant("B")))))
      }
    };
    var net = Net(annotations);

    NetworkAssert.CheckSound(net);
  }

  [Fact]
  public static void MonolithicNetworkPassesChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {
        "A",
        Lang.Equals(Start)
      },
      {
        "B",
        Lang.Until(new BigInteger(1), Lang.IsNone<Bgp>(),
          Lang.IfSome<Bgp>(b =>
            And(b.GetAsLength() == new BigInteger(1), b.GetLp() == new BigInteger(100),
              b.GetTags() == FBag.Create(Constant("A")))))
      },
      {
        "C",
        Lang.Until(new BigInteger(2), Lang.IsNone<Bgp>(),
          Lang.IfSome<Bgp>(b =>
            And(b.GetAsLength() == new BigInteger(2), b.GetLp() == new BigInteger(100),
              b.GetTags() == FBag.Create(Constant("A"), Constant("B")))))
      }
    };
    var net = Net(annotations);

    NetworkAssert.CheckSoundMonolithic(net);
  }
}
