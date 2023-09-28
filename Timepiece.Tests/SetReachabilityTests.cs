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
///   Tests for networks where routes are CSets of node names.
/// </summary>
public static class SetReachabilityTests
{
  private static AnnotatedNetwork<CSet<TV>, TV> Net<TV>(Digraph<TV> digraph, BigInteger convergeTime,
    Dictionary<TV, Func<Zen<CSet<TV>>, Zen<BigInteger>, Zen<bool>>> annotations) where TV : notnull
  {
    var transferFunctions = digraph.MapEdges<Func<Zen<CSet<TV>>, Zen<CSet<TV>>>>(_ => r => r);
    var initialValues = digraph.MapNodes(n => CSet.Empty<TV>().Add(n));

    var property = ContainsAll(digraph);
    var monolithicProperties = digraph.MapNodes(_ => property);
    var modularProperties = digraph.MapNodes(_ => Lang.Finally(convergeTime, property));
    return new AnnotatedNetwork<CSet<TV>, TV>(digraph, transferFunctions, CSet.Union, initialValues, annotations,
      modularProperties, monolithicProperties, Array.Empty<SymbolicValue<Unit>>());
  }

  private static Func<Zen<CSet<TV>>, Zen<bool>> ContainsAll<TV>(Digraph<TV> digraph) where TV : notnull
  {
    return route =>
      digraph.FoldNodes(Zen.True(), (b, n) => Zen.And(b, route.Contains(n)));
  }

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
    var topology = new Digraph<string>(new Dictionary<string, List<string>>
    {
      {"A", new List<string> {"B", "C"}},
      {"B", new List<string> {"A", "C", "D"}},
      {"C", new List<string> {"A", "B", "D"}},
      {"D", new List<string> {"B", "C"}}
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
      }
    };
    var net = Net(topology, convergeTime, annotations);
    NetworkAssert.CheckSound(net);
  }

  [Fact]
  public static void SquareUnsoundAnnotationsFailChecks()
  {
    var topology = new Digraph<string>(new Dictionary<string, List<string>>
    {
      {"A", new List<string> {"B", "C"}},
      {"B", new List<string> {"A", "D"}},
      {"C", new List<string> {"A", "D"}},
      {"D", new List<string> {"B", "C"}}
    });
    var allNodes = topology.FoldNodes(CSet.Empty<string>(), CSet.Add);
    var convergeTime = new BigInteger(3);
    // won't work: can't prove after predicate using the before predicate
    var annotations = topology.MapNodes(n => Lang.Until(new BigInteger(2),
      route => Zen.And(route.Contains(n), route.IsSubsetOf(allNodes)),
      // route.IsSubsetOf(topology[n].Aggregate(CSet.Empty<string>().Add(n), (r, m) => r.Add(m)))),
      ContainsAll(topology)));
    var net = Net(topology, convergeTime, annotations);
    NetworkAssert.CheckUnsoundCheck(net, SmtCheck.Inductive);
  }

  [Fact]
  public static void SquareSoundAnnotationsPassChecks()
  {
    var topology = new Digraph<string>(new Dictionary<string, List<string>>
    {
      {"A", new List<string> {"B", "C"}},
      {"B", new List<string> {"A", "D"}},
      {"C", new List<string> {"A", "D"}},
      {"D", new List<string> {"B", "C"}}
    });
    var convergeTime = new BigInteger(3);
    // Observe that these annotations do not use Until, as they require two ifs.
    var annotations = topology.MapNodes<Func<Zen<CSet<string>>, Zen<BigInteger>, Zen<bool>>>(n =>
      (route, time) => Zen.If(time < new BigInteger(1),
        CSet.Empty<string>().Add(n).IsSubsetOf(route),
        Zen.If(time < new BigInteger(2),
          topology[n].Aggregate(CSet.Empty<string>().Add(n), CSet.Add).IsSubsetOf(route),
          ContainsAll(topology)(route))));
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
