using System;
using System.Collections.Generic;
using System.Numerics;
using Karesansui.Networks;
using Xunit;
using ZenLib;
using static ZenLib.Zen;

namespace Karesansui.Tests;

public static class BooleanTests
{
  private static BooleanNetwork<Unit> Net(
    Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Default.Path(2);

    var initialValues = topology.ForAllNodes(n => Eq<string>(n, "A"));

    var convergeTime = new BigInteger(2);
    return new BooleanNetwork<Unit>(topology, initialValues, annotations, Array.Empty<SymbolicValue<Unit>>(),
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
}
