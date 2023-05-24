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
  private static AnnotatedNetwork<bool, TS> BooleanAnnotatedNetwork<TS>(Network<bool, TS> net,
    Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations,
    BigInteger convergeTime) => new(net, annotations,
    net.Topology.MapNodes(_ => Lang.Finally(convergeTime, Lang.Identity<bool>())),
    net.Topology.MapNodes(_ => Lang.Identity<bool>()));

  private static AnnotatedNetwork<bool, Unit> Net(
    Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Topologies.Path(2);

    var initialValues = topology.MapNodes(n => Eq<string>(n, "A"));
    var net = new BooleanNetwork<Unit>(topology, initialValues, Array.Empty<SymbolicValue<Unit>>());

    var convergeTime = new BigInteger(2);
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
    var net = Net(annotations);

    NetworkAssert.CheckSound(net);
    // Assert.False(net.CheckAnnotations().HasValue, "Sound boolean annotations should pass checks.");
  }

  [Fact]
  public static void UnsoundAnnotationsFailChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Globally(Lang.Identity<bool>())},
      {"B", Lang.Globally(Lang.Identity<bool>())}
    };
    var net = Net(annotations);

    NetworkAssert.CheckUnsound(net);
    // Assert.True(net.CheckAnnotations().HasValue, "Unsound boolean annotations should fail checks.");
  }

  [Fact]
  public static void FatTreeMonoChecks()
  {
    var topology = Topologies.FatTree(4);
    var initialValues =
      topology.MapNodes(n => Constant(n == FatTree.FatTreeLayer.Edge.Node(19)));
    var net = new BooleanNetwork<Unit>(topology, initialValues, Array.Empty<SymbolicValue<Unit>>());
    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>();
    var annotatedNetwork = BooleanAnnotatedNetwork(net, annotations, new BigInteger(4));
    // change edge-7's annotation to a bad property
    annotatedNetwork.MonolithicProperties[FatTree.FatTreeLayer.Edge.Node(7)] = _ => False();
    NetworkAssert.CheckUnsoundCheck(annotatedNetwork, SmtCheck.Monolithic);
  }
}
