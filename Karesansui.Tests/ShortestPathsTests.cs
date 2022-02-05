using System;
using System.Collections.Generic;
using System.Numerics;
using Karesansui;
using Karesansui.Networks;
using Xunit;
using ZenLib;

namespace Karesansui.Tests;

public class ShortestPathsTests
{
    private static ShortestPath<Unit> Simple(
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
          new Dictionary<Zen<Unit>, Func<Zen<Unit>, Zen<bool>>>(), 4);
    }

    [Fact]
    public void SoundAnnotationsPassChecks()
    {
        var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals<Option<BigInteger>>(Option.Some(new BigInteger(0U)))},
      {"B", Lang.Finally(new BigInteger(0U), Lang.IsSome<BigInteger>())},
      {"C", Lang.Finally(new BigInteger(1U), Lang.IsSome<BigInteger>())}
    };
        var net = Simple(annotations);

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
        var net = Simple(annotations);

        Assert.False(net.CheckAnnotations(), "Unsound annotations for simple shortest-paths should fail.");
    }
}