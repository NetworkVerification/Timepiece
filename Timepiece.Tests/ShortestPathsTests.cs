using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.Networks;
using Xunit;
using ZenLib;

namespace Timepiece.Tests;

public static class ShortestPathsTests
{
  private static ShortestPath<Unit> NonSymbolic(
    Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Topologies.Path(3);

    var initialValues = new Dictionary<string, Zen<Option<BigInteger>>>
    {
      {"A", Option.Some(new BigInteger(0))},
      {"B", Option.None<BigInteger>()},
      {"C", Option.None<BigInteger>()}
    };

    var safetyProperties = topology.MapNodes(_ => Lang.IsSome<BigInteger>());
    return new ShortestPath<Unit>(topology, initialValues, annotations,
      System.Array.Empty<SymbolicValue<Unit>>(), 4, safetyProperties);
  }

  private static ShortestPath<Unit> NonSymbolicPathLength(
    Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Topologies.Path(3);

    var initialValues = new Dictionary<string, Zen<Option<BigInteger>>>
    {
      {"A", Option.Some(new BigInteger(0))},
      {"B", Option.None<BigInteger>()},
      {"C", Option.None<BigInteger>()}
    };

    var safetyProperties = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<bool>>>
    {
      {"A", Lang.IfSome<BigInteger>(r => r == BigInteger.Zero)},
      {"B", Lang.IfSome<BigInteger>(r => r == BigInteger.One)},
      {"C", Lang.IfSome<BigInteger>(r => r == new BigInteger(2))},
    };
    return new ShortestPath<Unit>(topology, initialValues, annotations,
      System.Array.Empty<SymbolicValue<Unit>>(), 4, safetyProperties);
  }

  private static readonly SymbolicValue<BigInteger> D = new("d", r => r >= BigInteger.Zero);

  private static ShortestPath<BigInteger> SymbolicDest(
    Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Topologies.Complete(3);

    var initialValues = new Dictionary<string, Zen<Option<BigInteger>>>
    {
      {"A", Option.Create(D.Value)},
      {"B", Option.None<BigInteger>()},
      {"C", Option.None<BigInteger>()}
    };

    var symbolics = new[] {D};

    var safetyProperties = topology.MapNodes(_ => Lang.IsSome<BigInteger>());
    return new ShortestPath<BigInteger>(topology, initialValues, annotations, symbolics, 2, safetyProperties);
  }

  [Fact]
  public static void SoundAnnotationsPassChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals<Option<BigInteger>>(Option.Some(new BigInteger(0)))},
      {
        "B",
        Lang.Until(new BigInteger(1), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r == new BigInteger(1)))
      },
      {
        "C",
        Lang.Until(new BigInteger(2), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r == new BigInteger(2)))
      }
    };
    var net = NonSymbolic(annotations);

    NetworkAssert.CheckSound(net);
  }

  [Fact]
  public static void SoundPathLengthAnnotationsPassChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals<Option<BigInteger>>(Option.Some(new BigInteger(0)))},
      {
        "B",
        Lang.Until(new BigInteger(1), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r == new BigInteger(1)))
      },
      {
        "C",
        Lang.Until(new BigInteger(2), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r == new BigInteger(2)))
      }
    };
    var net = NonSymbolicPathLength(annotations);
    NetworkAssert.CheckSound(net);
  }

  [Fact]
  public static void UnsoundAnnotationsFailChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals<Option<BigInteger>>(Option.Some(new BigInteger(0)))},
      {"B", Lang.Never(Lang.IsSome<BigInteger>())},
      {"C", Lang.Never(Lang.IsSome<BigInteger>())}
    };
    var net = NonSymbolic(annotations);

    NetworkAssert.CheckUnsound(net);
  }

  [Fact]
  public static void SoundSymbolicAnnotationsPassChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals(Option.Create(D.Value))},
      {"B", Lang.Until(new BigInteger(1), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r >= D.Value))},
      {"C", Lang.Until(new BigInteger(1), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r >= D.Value))}
    };
    var net = SymbolicDest(annotations);

    NetworkAssert.CheckSound(net);
  }

  [Fact]
  public static void UnsoundSymbolicAnnotationsFailChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Globally(Lang.IfSome<BigInteger>(r => r <= D.Value))},
      {"B", Lang.Finally(new BigInteger(1), Lang.IfSome<BigInteger>(r => r <= D.Value))},
      {"C", Lang.Finally(new BigInteger(1), Lang.IfSome<BigInteger>(r => r <= D.Value))}
    };
    var net = SymbolicDest(annotations);

    NetworkAssert.CheckUnsound(net);
  }
}
