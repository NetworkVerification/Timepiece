using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.Networks;
using Xunit;
using ZenLib;
using static ZenLib.Zen;
using Array = System.Array;

namespace Timepiece.Tests;

public static class BooleanTests
{
  private static AnnotatedNetwork<bool, NodeType> BooleanAnnotatedNetwork<NodeType>(Network<bool, NodeType> net,
    Dictionary<NodeType, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations,
    Zen<BigInteger> convergeTime) where NodeType : notnull => new(net, annotations,
    net.Digraph.MapNodes(_ => Lang.Finally(convergeTime, Lang.Identity<bool>())),
    net.Digraph.MapNodes(_ => Lang.Identity<bool>()));

  private static AnnotatedNetwork<bool, string> Net(
    Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations,
    Zen<BigInteger> convergeTime)
  {
    var topology = Topologies.Path(2);

    var initialValues = topology.MapNodes(n => Eq<string>(n, "A"));
    var net = new BooleanNetwork<string>(topology, initialValues, Array.Empty<ISymbolic>());

    return BooleanAnnotatedNetwork(net, annotations, convergeTime);
  }

  [Fact]
  public static void SoundAnnotationsPassChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Globally(Lang.Identity<bool>())},
      {"B", Lang.Finally(new BigInteger(1), Lang.Identity<bool>())}
    };
    var net = Net(annotations, new BigInteger(2));

    NetworkAssert.CheckSound(net);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  [InlineData(100)]
  [InlineData(10000)]
  public static void SoundAnnotationsPassChecksDelayedMaxDelayConcrete(int max)
  {
    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Finally(new BigInteger(1), Lang.Identity<bool>())},
      {"B", Lang.Finally(new BigInteger(1 + max), Lang.Identity<bool>())}
    };
    // need the converge time to be the largest witness time, i.e. B's
    var net = Net(annotations, new BigInteger(1 + max));
    net.MaxDelay = new BigInteger(max);

    NetworkAssert.CheckSoundDelayed(net);
  }

  /// <summary>
  /// Test that the annotations pass for any symbolically-chosen maximum delay for a 3-node path network.
  /// </summary>
  [Fact]
  public static void SoundAnnotationsPassChecksDelayedMaxDelaySymbolic()
  {
    var topology = Topologies.Path(3);
    var initialValues = topology.MapNodes(n => Eq<string>(n, "A"));
    var delay = new SymbolicValue<BigInteger>("delay", d => d >= BigInteger.One);
    var net = new BooleanNetwork<string>(topology, initialValues, new ISymbolic[] {delay});

    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Finally(new BigInteger(1), Lang.Identity<bool>())},
      {"B", Lang.Finally(new BigInteger(1) + delay.Value, Lang.Identity<bool>())},
      {"C", Lang.Finally(new BigInteger(1) + new BigInteger(2) * delay.Value, Lang.Identity<bool>())},
    };
    // need the converge time to be the largest witness time, i.e. C's
    var annotated = BooleanAnnotatedNetwork(net, annotations, new BigInteger(2) * delay.Value + BigInteger.One);
    // NOTE: if we wanted, we could generalize this further to have an arbitrary delay on each edge
    annotated.MaxDelay = delay.Value;

    NetworkAssert.CheckSoundDelayed(annotated);
  }

  [Fact]
  public static void UnsoundAnnotationsFailChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Globally(Lang.Identity<bool>())},
      {"B", Lang.Globally(Lang.Identity<bool>())}
    };
    var net = Net(annotations, new BigInteger(2));

    NetworkAssert.CheckUnsound(net);
  }

  [Fact]
  public static void FatTreeMonoChecks()
  {
    var topology = Topologies.FatTree(4);
    var initialValues =
      topology.MapNodes(n => Constant(n == FatTree.FatTreeLayer.Edge.Node(19)));
    var net = new BooleanNetwork<string>(topology, initialValues, Array.Empty<ISymbolic>());
    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>();
    var annotatedNetwork = BooleanAnnotatedNetwork(net, annotations, new BigInteger(4));
    // change edge-7's annotation to a bad property
    annotatedNetwork.MonolithicProperties[FatTree.FatTreeLayer.Edge.Node(7)] = _ => False();
    NetworkAssert.CheckUnsoundCheck(annotatedNetwork, SmtCheck.Monolithic);
  }
}
