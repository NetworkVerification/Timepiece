using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.Datatypes;
using Timepiece.Networks;
using Xunit;
using ZenLib;
using static ZenLib.Zen;
using Array = System.Array;

namespace Timepiece.Tests;

public static class BgpNetworkTests
{
  private static readonly Zen<Option<Bgp>> Start = Option.Create<Bgp>(new Bgp());

  private static BgpAnnotatedNetwork Net(
    Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Topologies.Path(3);

    var initialValues = new Dictionary<string, Zen<Option<Bgp>>>
    {
      {"A", Start},
      {"B", Option.None<Bgp>()},
      {"C", Option.None<Bgp>()}
    };

    var convergeTime = new BigInteger(2);

    return new BgpAnnotatedNetwork(topology, initialValues, annotations, convergeTime,
      Array.Empty<SymbolicValue<Bgp>>());
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
            And(b.GetAsLength() == new BigInteger(2), b.GetLp() == new BigInteger(100), b.HasTag("B"))))
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
              b.GetTags() == CSet.Empty<string>().Add("A"))))
      },
      {
        "C",
        Lang.Until(new BigInteger(2), Lang.IsNone<Bgp>(),
          Lang.IfSome<Bgp>(b =>
            And(b.GetAsLength() == new BigInteger(2), b.GetLp() == new BigInteger(100),
              b.GetTags() == CSet.Empty<string>().Add("A").Add("B"))))
      }
    };
    var net = Net(annotations);

    NetworkAssert.CheckSound(net);
  }

  [Fact]
  public static void MonolithicNetworkPassesExactChecks()
  {
    var net = Net(new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>());
    NetworkAssert.CheckSoundMonolithic(net);
  }

  [Fact]
  public static void UnsoundAnnotationsFailChecks()
  {
    var annotations =
      new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
      {
        {"A", Lang.Equals(Start)},
        {"B", Lang.Finally<Option<Bgp>>(new BigInteger(1), r => r == Start)},
        {"C", Lang.Finally<Option<Bgp>>(new BigInteger(2), r => r == Start)}
      };
    var net = Net(annotations);
    NetworkAssert.CheckUnsoundCheck(net, SmtCheck.Inductive);
  }

  [Fact]
  public static void MonolithicNetworkFailsBadChecks()
  {
    var net = Net(new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>());
    net.MonolithicProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<bool>>>
    {
      {"A", r => r == Start},
      {"B", r => r == Start},
      {"C", r => r == Start}
    };
    NetworkAssert.CheckUnsoundCheck(net, SmtCheck.Monolithic);
  }

  [Fact]
  public static void BiggerMonolithicNetworkFailsBadChecks()
  {
    var topology = Topologies.FatTree(4);
    Dictionary<string, Zen<Option<Bgp>>> initialValues =
      topology.MapNodes(n => n == FatTree.FatTreeLayer.Edge.Node(19) ? Start : Option.None<Bgp>());
    var annotations = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>();
    var net = new BgpAnnotatedNetwork(topology, initialValues, annotations, new BigInteger(4),
      Array.Empty<SymbolicValue<Bgp>>())
    {
      MonolithicProperties =
      {
        [FatTree.FatTreeLayer.Edge.Node(7)] = _ => False()
      }
    };
    NetworkAssert.CheckUnsoundCheck(net, SmtCheck.Monolithic);
  }
}
