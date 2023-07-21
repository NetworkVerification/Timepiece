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
  private static AnnotatedNetwork<bool, TV, TS> BooleanAnnotatedNetwork<TV, TS>(Network<bool, TV, TS> net,
    Dictionary<TV, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations,
    BigInteger convergeTime) where TV : notnull => new(net, annotations,
    net.Digraph.MapNodes(_ => Lang.Finally(convergeTime, Lang.Identity<bool>())),
    net.Digraph.MapNodes(_ => Lang.Identity<bool>()));

  private static AnnotatedNetwork<bool, string, Unit> Net(
    Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Topologies.Path(2);

    var initialValues = topology.MapNodes(n => Eq<string>(n, "A"));
    var net = new BooleanNetwork<string, Unit>(topology, initialValues, Array.Empty<SymbolicValue<Unit>>());

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
  }

  [Fact]
  public static void FatTreeMonoChecks()
  {
    var topology = Topologies.FatTree(4);
    var initialValues =
      topology.MapNodes(n => Constant(n == FatTree.FatTreeLayer.Edge.Node(19)));
    var net = new BooleanNetwork<string, Unit>(topology, initialValues, Array.Empty<SymbolicValue<Unit>>());
    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>();
    var annotatedNetwork = BooleanAnnotatedNetwork(net, annotations, new BigInteger(4));
    // change edge-7's annotation to a bad property
    annotatedNetwork.MonolithicProperties[FatTree.FatTreeLayer.Edge.Node(7)] = _ => False();
    NetworkAssert.CheckUnsoundCheck(annotatedNetwork, SmtCheck.Monolithic);
  }
}
