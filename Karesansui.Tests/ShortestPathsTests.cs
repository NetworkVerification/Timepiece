using System;
using System.Collections.Generic;
using System.Numerics;
using Karesansui.Networks;
using Xunit;
using ZenLib;

namespace Karesansui.Tests;

public class ShortestPathsTests
{
  private static ShortestPath<Unit> NonSymbolic(
    Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Default.Path(3);

    var initialValues = new Dictionary<string, Zen<Option<BigInteger>>>
    {
      {"A", Option.Some(new BigInteger(0))},
      {"B", Option.None<BigInteger>()},
      {"C", Option.None<BigInteger>()}
    };

    return new ShortestPath<Unit>(topology, initialValues, annotations,
      Array.Empty<SymbolicValue<Unit>>(), 4);
  }

  private static readonly SymbolicValue<BigInteger> d = new("d", r => r >= BigInteger.Zero);

  public static ShortestPath<BigInteger> SymbolicDest(
    Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Default.Complete(3);

    var initialValues = new Dictionary<string, Zen<Option<BigInteger>>>
    {
      {"A", Language.Some(d.Value)},
      {"B", Option.None<BigInteger>()},
      {"C", Option.None<BigInteger>()}
    };

    var symbolics = new[] {d};

    return new ShortestPath<BigInteger>(topology, initialValues, annotations, symbolics, 2);
  }

  [Fact]
  public void SoundAnnotationsPassChecks()
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

    Assert.True(net.CheckAnnotations(), "Sound annotations for simple shortest-paths should pass.");
  }

  [Fact]
  public void UnsoundAnnotationsFailChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals<Option<BigInteger>>(Option.Some(new BigInteger(0)))},
      {"B", Lang.Never(Lang.IsSome<BigInteger>())},
      {"C", Lang.Never(Lang.IsSome<BigInteger>())}
    };
    var net = NonSymbolic(annotations);

    Assert.False(net.CheckAnnotations(), "Unsound annotations for simple shortest-paths should fail.");
  }

  [Fact]
  public void SoundSymbolicAnnotationsPassChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals(Language.Some(d.Value))},
      {"B", Lang.Until(new BigInteger(1), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r >= d.Value))},
      {"C", Lang.Until(new BigInteger(2), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r >= d.Value))}
    };
    var net = NonSymbolic(annotations);

    Assert.True(net.CheckAnnotations(), "Sound annotations for symbolic shortest-paths should pass.");
  }
}
