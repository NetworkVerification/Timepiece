using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.DataTypes;
using Timepiece.Tests.Networks;
using Xunit;
using ZenLib;
using static ZenLib.Zen;
using Array = System.Array;

namespace Timepiece.Tests;

public static class BgpNetworkTests
{
  private static readonly Zen<Option<Bgp>> Start = Option.Create<Bgp>(new Bgp());

  /// <summary>
  ///   Construct a map of nodes to predicates such that every node's predicate is that it is reachable
  ///   (has some route).
  /// </summary>
  /// <param name="digraph"></param>
  /// <returns></returns>
  private static Dictionary<string, Func<Zen<Option<Bgp>>, Zen<bool>>> Reachable(Digraph<string> digraph)
  {
    return digraph.MapNodes(_ => Lang.IsSome<Bgp>());
  }

  private static BgpAnnotatedNetwork<string> Net(
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
    var monolithicProperties = Reachable(topology);
    var modularProperties = topology.MapNodes(n => Lang.Finally(convergeTime, monolithicProperties[n]));

    return new BgpAnnotatedNetwork<string>(topology, initialValues, annotations, modularProperties,
      monolithicProperties,
      Array.Empty<ISymbolic>());
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
  public static void FatTreeNetworkFailsBadMonolithicCheck()
  {
    var topology = Topologies.FatTree(4);
    Dictionary<string, Zen<Option<Bgp>>> initialValues =
      topology.MapNodes(n => n == FatTree.FatTreeLayer.Edge.Node(19) ? Start : Option.None<Bgp>());
    var monolithicProperties = Reachable(topology);
    // break a safety check
    monolithicProperties[FatTree.FatTreeLayer.Core.Node(0)] = _ => False();
    var modularProperties = topology.MapNodes(n => Lang.Finally(new BigInteger(4), monolithicProperties[n]));
    // skip constructing the annotations since we're just testing the monolithic check
    var annotations = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>();
    var net = new BgpAnnotatedNetwork<string>(topology, initialValues, annotations, modularProperties,
      monolithicProperties,
      Array.Empty<ISymbolic>());
    NetworkAssert.CheckUnsoundCheck(net, SmtCheck.Monolithic);
  }

  /// <summary>
  ///   A digraph representing Figure 1 of the Lightyear paper (see page 2).
  /// </summary>
  /// <returns></returns>
  private static Digraph<string> LightyearFigure1()
  {
    return new Digraph<string>(new Dictionary<string, List<string>>
    {
      {"R1", new List<string> {"R2", "R3", "ISP1"}},
      {"R2", new List<string> {"R1", "R3", "ISP2"}},
      {"R3", new List<string> {"R1", "R2", "Customer"}},
      {"ISP1", new List<string> {"R1"}},
      {"ISP2", new List<string> {"R2"}},
      {"Customer", new List<string> {"R3"}}
    });
  }

  private static Dictionary<(string, string), Func<Zen<Option<Bgp>>, Zen<Option<Bgp>>>> LightyearTransfer()
  {
    return new Dictionary<(string, string), Func<Zen<Option<Bgp>>, Zen<Option<Bgp>>>>
    {
      {("ISP1", "R1"), Lang.Omap<Bgp, Bgp>(r => r.IncrementAsLength().AddTag("100:1"))},
      {
        ("R2", "ISP2"),
        Lang.Bind<Bgp, Bgp>(r => If(r.HasTag("100:1"), Option.None<Bgp>(), Option.Create(r.IncrementAsLength())))
      }
      // TODO: rest use defaults
    };
  }
}
