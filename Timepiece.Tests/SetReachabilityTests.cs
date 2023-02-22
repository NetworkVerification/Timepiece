using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Timepiece.Networks;
using Xunit;
using ZenLib;
using Array = System.Array;

namespace Timepiece.Tests;

/// <summary>
/// Tests for networks where routes are CSets of node names.
/// </summary>
public static class SetReachabilityTests
{
  private static Network<CSet<string>, Unit> Net(Topology topology, BigInteger convergeTime,
    Dictionary<string, Func<Zen<CSet<string>>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var transferFunctions = topology.MapEdges<Func<Zen<CSet<string>>, Zen<CSet<string>>>>(_ => r => r);
    var initialValues = topology.MapNodes(n => CSet.Empty<string>().Add(n));

    var property = ContainsAll(topology);
    var monolithicProperties = topology.MapNodes(_ => property);
    var modularProperties = topology.MapNodes(_ => Lang.Finally(convergeTime, property));
    return new Network<CSet<string>, Unit>(topology, transferFunctions, CSet.Union, initialValues, annotations,
      modularProperties, monolithicProperties, Array.Empty<SymbolicValue<Unit>>());
  }

  private static Func<Zen<CSet<string>>, Zen<bool>> ContainsAll(Topology topology) => route =>
    topology.FoldNodes(Zen.True(), (b, n) => Zen.And(b, route.Contains(n)));

  [Fact]
  public static void SoundAnnotationsPassChecks()
  {
    var topology = Topologies.Complete(3);
    var convergeTime = new BigInteger(2);
    var annotations =
      topology.MapNodes(n => Lang.Until(new BigInteger(1), route => route.Contains(n), ContainsAll(topology)));
    var net = Net(topology, convergeTime, annotations);

    NetworkAssert.CheckSound(net);
    // Assert.False(net.CheckAnnotations().HasValue, "Sound boolean annotations should pass checks.");
  }

  [Fact]
  public static void DiamondSoundAnnotationsPassChecks()
  {
    var topology = new Topology(new Dictionary<string, List<string>>
    {
      {"A", new List<string> {"B", "C"}},
      { "B", new List<string> {"A", "C", "D"} },
      { "C", new List<string> {"A", "B", "D"}},
      { "D", new List<string> {"B", "C"}}
    });
    var convergeTime = new BigInteger(3);
    var annotations = new Dictionary<string, Func<Zen<CSet<string>>, Zen<BigInteger>, Zen<bool>>>
    {
      {
        "A", Lang.Until(new BigInteger(2),
          route => Zen.And(route.Contains("A"), route.IsSubsetOf(CSet.Empty<string>().Add("A").Add("B").Add("C"))),
          ContainsAll(topology))
      },
      {"B", Lang.Until(new BigInteger(1), route => route == CSet.Empty<string>().Add("B"), ContainsAll(topology))},
      {"C", Lang.Until(new BigInteger(1), route => route == CSet.Empty<string>().Add("C"), ContainsAll(topology))},
      {
        "D", Lang.Until(new BigInteger(2),
          route => Zen.And(route.Contains("D"), route.IsSubsetOf(CSet.Empty<string>().Add("D").Add("B").Add("C"))),
          ContainsAll(topology))
      },
    };
    var net = Net(topology, convergeTime, annotations);
    NetworkAssert.CheckSound(net);
  }

  [Fact]
  public static void SquareSoundAnnotationsPassChecks()
  {
    var topology = new Topology(new Dictionary<string, List<string>>
    {
      {"A", new List<string> {"B", "C"}},
      {"B", new List<string> {"A", "D"}},
      {"C", new List<string> {"A", "D"}},
      {"D", new List<string> {"B", "C"}}
    });
    var convergeTime = new BigInteger(3);
    // doesn't work: ctex shows a case where nodes contain all neighbors
    // could we weaken the before condition to include the case that all of the neighbors are there?
    var annotations = topology.MapNodes(n => Lang.Until(new BigInteger(2),
      route => Zen.And(route.Contains(n),
        route.IsSubsetOf(topology[n].Aggregate(CSet.Empty<string>().Add(n), (r, m) => r.Add(m)))),
      ContainsAll(topology)));
    var net = Net(topology, convergeTime, annotations);
    NetworkAssert.CheckSound(net);
  }

  [Fact]
  public static void UnsoundAnnotationsFailChecks()
  {
    var topology = Topologies.Complete(3);
    var convergeTime = new BigInteger(2);
    var annotations =
      topology.MapNodes(_ => Lang.Globally(ContainsAll(topology)));
    var net = Net(topology, convergeTime, annotations);

    NetworkAssert.CheckUnsound(net);
    // Assert.True(net.CheckAnnotations().HasValue, "Unsound boolean annotations should fail checks.");
  }
}
