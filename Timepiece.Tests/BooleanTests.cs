using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.Networks;
using Xunit;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Tests;

public static class BooleanTests
{
  private static BooleanNetwork<Unit> Net(
    Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Topologies.Path(2);

    var initialValues = topology.ForAllNodes(n => Eq<string>(n, "A"));

    var convergeTime = new BigInteger(2);
    return new BooleanNetwork<Unit>(topology, initialValues, annotations, System.Array.Empty<SymbolicValue<Unit>>(),
      convergeTime);
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
      {"B", Lang.Globally(Lang.Identity<bool>())},
    };
    var net = Net(annotations);

    NetworkAssert.CheckUnsound(net);
    // Assert.True(net.CheckAnnotations().HasValue, "Unsound boolean annotations should fail checks.");
  }

  [Fact]
  public static void FattreeMonoChecks()
  {
    var topology = Topologies.FatTree(4);
    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>();
    Dictionary<string, Zen<bool>> initialValues =
      topology.ForAllNodes(n => Constant(n == FatTree.FatTreeLayer.Edge.Node(19)));
    var net = new BooleanNetwork<Unit>(topology, initialValues, annotations,
      System.Array.Empty<SymbolicValue<Unit>>(), new BigInteger(4))
    {
      MonolithicProperties =
      {
        [FatTree.FatTreeLayer.Edge.Node(7)] = _ => False()
      }
    };
    NetworkAssert.CheckUnsoundCheck(net, SmtCheck.Monolithic);
  }
}
