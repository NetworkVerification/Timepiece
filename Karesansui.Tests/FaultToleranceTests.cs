using System;
using System.Collections.Generic;
using System.Numerics;
using Karesansui.Networks;
using Xunit;
using ZenLib;

namespace Karesansui.Tests;

public static class FaultToleranceTests
{
  public static FaultTolerance<Unit> UnitFtNet(
    Dictionary<string, Func<Zen<Option<Unit>>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Default.Complete(3);

    var initialValues = new Dictionary<string, Zen<Option<Unit>>>
    {
      {"A", Option.Some(new Unit())},
      {"B", Option.None<Unit>()},
      {"C", Option.None<Unit>()}
    };

    var modularProperties = topology.ForAllNodes(_ => Lang.Finally(new BigInteger(2), Lang.IsSome<Unit>()));
    var monolithicProperties = topology.ForAllNodes(_ => Lang.IsSome<Unit>());

    var failedEdges = Zen.Symbolic<FSeq<(string, string)>>(topology.NEdges);

    return new FaultTolerance<Unit>(new UnitNetwork(topology), initialValues, annotations, modularProperties,
      monolithicProperties, failedEdges);
  }

  [Fact]
  public static void SoundAnnotationsPassChecks()
  {
    // FIXME: these annotations are too weak: we need to state that if the one edge is failed, then the other one will work,
    // so that either B gets a route at 1 and C gets a route at 2, or C gets a route at 1 and B gets a route at 2,
    // or both B and C get a route at 1
    var annotations = new Dictionary<string, Func<Zen<Option<Unit>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Globally(Lang.IsSome<Unit>())},
      {"B", Lang.Finally(new BigInteger(2), Lang.IsSome<Unit>())},
      {"C", Lang.Finally(new BigInteger(2), Lang.IsSome<Unit>())}
    };

    var net = UnitFtNet(annotations);

    NetworkAssert.CheckSound(net);
  }
}
