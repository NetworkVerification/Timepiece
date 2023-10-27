using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.Tests.Networks;
using Xunit;
using ZenLib;

namespace Timepiece.Tests;

using TaggedRoute = Pair<BigInteger, bool>;

public static class HijackTests
{
  private static Hijack Net(
    Func<SymbolicValue<Option<TaggedRoute>>,
      Dictionary<string, Func<Zen<Option<TaggedRoute>>, Zen<BigInteger>, Zen<bool>>>> annotations)
  {
    var topology = Topologies.Path(4);
    const string dest = "A";
    const string hijacker = "D";
    var convergeTime = new BigInteger(4);

    return new Hijack(topology, hijacker, dest, annotations, convergeTime);
  }

  [Fact]
  public static void SoundAnnotationsPassChecks()
  {
    var annotations =
      new Func<SymbolicValue<Option<TaggedRoute>>,
        Dictionary<string, Func<Zen<Option<TaggedRoute>>, Zen<BigInteger>, Zen<bool>>>>(_ =>
        new Dictionary<string, Func<Zen<Option<TaggedRoute>>, Zen<BigInteger>, Zen<bool>>>
        {
          {"A", Lang.Globally<Option<TaggedRoute>>(Hijack.HasInternalRoute)},
          {"B", Lang.Finally<Option<TaggedRoute>>(BigInteger.One, Hijack.HasInternalRoute)},
          {"C", Lang.Finally<Option<TaggedRoute>>(new BigInteger(2), Hijack.HasInternalRoute)},
          {"D", Lang.Globally<Option<TaggedRoute>>(_ => true)}
        });
    var net = Net(annotations);

    NetworkAsserts.Sound(net);
  }
}
