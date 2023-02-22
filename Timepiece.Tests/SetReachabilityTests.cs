using System;
using System.Collections.Generic;
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
  private static Network<CSet<string>, Unit> Net(Topology topology,
    Dictionary<string, Func<Zen<CSet<string>>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var transferFunctions = topology.MapEdges<Func<Zen<CSet<string>>, Zen<CSet<string>>>>(_ => r => r);
    var initialValues = topology.MapNodes(n => CSet.Empty<string>().Add(n));

    var convergeTime = new BigInteger(2);
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
    var annotations =
      topology.MapNodes(n => Lang.Until(new BigInteger(1), route => route.Contains(n), ContainsAll(topology)));
    var net = Net(topology, annotations);

    NetworkAssert.CheckSound(net);
    // Assert.False(net.CheckAnnotations().HasValue, "Sound boolean annotations should pass checks.");
  }

  [Fact]
  public static void UnsoundAnnotationsFailChecks()
  {
    var topology = Topologies.Complete(3);
    var annotations =
      topology.MapNodes(_ => Lang.Globally(ContainsAll(topology)));
    var net = Net(topology, annotations);

    NetworkAssert.CheckUnsound(net);
    // Assert.True(net.CheckAnnotations().HasValue, "Unsound boolean annotations should fail checks.");
  }
}
