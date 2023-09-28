using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.Networks;
using Xunit;
using ZenLib;

namespace Timepiece.Tests;

// Routes are options of LP and path length
using Route = Pair<BigInteger, BigInteger>;

/// <summary>
/// Tests based on examples presented in Tim Alberdingk Thijm's dissertation, "Modular Control Plane Verification".
/// </summary>
public static class LpDiamondTests
{
  private static readonly Digraph<string> Digraph = new(new Dictionary<string, List<string>>
  {
    {"a", new List<string>()},
    {"b", new List<string> {"a", "c"}},
    {"c", new List<string> {"a", "b"}},
    {"d", new List<string> {"b", "c"}},
  });

  public static AnnotatedNetwork<Option<Route>, string> Net(
    Dictionary<string, Func<Zen<Option<Route>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Option<Route>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<string, Func<Zen<Option<Route>>, Zen<bool>>> monolithicProperties)
  {
    var initialValues = new Dictionary<string, Zen<Option<Route>>>
    {
      {"a", Option.Create<Route>(Pair.Create<BigInteger, BigInteger>(new BigInteger(100), BigInteger.Zero))},
      {"b", Option.Null<Route>()},
      {"c", Option.Null<Route>()},
      {"d", Option.Null<Route>()},
    };
    return new AnnotatedNetwork<Option<Route>, string>(Digraph, Transfer, Lang.Omap2<Route>(Merge),
      initialValues, annotations, modularProperties, monolithicProperties, System.Array.Empty<SymbolicValue<Unit>>());
  }

  private static Zen<Route> Merge(Zen<Route> r1, Zen<Route> r2) =>
    Zen.If(r1.Item1() > r2.Item1(), r1,
      Zen.If(r1.Item1() < r2.Item1(), r2,
        Zen.If(r1.Item2() < r2.Item2(), r1, r2)));

  private static Zen<Route> Increment(Zen<Route> r) =>
    Pair.Create(r.Item1(), r.Item2() + BigInteger.One);

  private static Dictionary<(string, string), Func<Zen<Option<Route>>, Zen<Option<Route>>>> Transfer =>
    Digraph.MapEdges(e => e switch
    {
      // drop routes from a to c
      ("a", "c") => Lang.Const(Option.None<Route>()),
      // d prefers routes from c over routes from b
      ("c", "d") => Lang.Omap<Route, Route>(r =>
        Pair.Create<BigInteger, BigInteger>(new BigInteger(150), r.Item2() + BigInteger.One)),
      // all other routes are just incremented and reset to LP 100
      _ => Lang.Omap<Route, Route>(Increment)
    });

  [Fact]
  public static void DHasTaggedRoute()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<Route>>, Zen<BigInteger>, Zen<bool>>>
    {
      // a always has Some {100, 0}
      {
        "a",
        Lang.Globally(Lang.IfSome<Route>(r => Zen.And(r.Item1() == new BigInteger(100), r.Item2() == BigInteger.Zero)))
      },
      // b gets {100, 1} at time 1
      {
        "b",
        Lang.Until(new BigInteger(1), Lang.IsNone<Route>(),
          Lang.IfSome<Route>(r => Zen.And(r.Item1() == new BigInteger(100), r.Item2() == BigInteger.One)))
      },
      // c gets {100, 2} at time 2
      {
        "c",
        Lang.Until(new BigInteger(2), Lang.IsNone<Route>(),
          Lang.IfSome<Route>(r => Zen.And(r.Item1() == new BigInteger(100), r.Item2() == new BigInteger(2))))
      },
      // d gets {100, 2} at time 2, but then {150, 3} at time 3
      {
        "d", Lang.Until(new BigInteger(3), Lang.OrSome<Route>(r => Zen.And(r.Item1() >= new BigInteger(0),
            r.Item1() < new BigInteger(150), r.Item2() > new BigInteger(1))),
          Lang.IfSome<Route>(r => Zen.And(r.Item2() == new BigInteger(3))))
      },
    };
    var modularProperties = new Dictionary<string, Func<Zen<Option<Route>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"a", Lang.Globally(Lang.True<Option<Route>>())},
      {"b", Lang.Globally(Lang.True<Option<Route>>())},
      {"c", Lang.Globally(Lang.True<Option<Route>>())},
      {"d", Lang.Finally(new BigInteger(3), Lang.IfSome<Route>(r => r.Item2() == new BigInteger(3)))},
    };
    var monolithicProperties = new Dictionary<string, Func<Zen<Option<Route>>, Zen<bool>>>
    {
      {"a", Lang.True<Option<Route>>()},
      {"b", Lang.True<Option<Route>>()},
      {"c", Lang.True<Option<Route>>()},
      {"d", Lang.IfSome<Route>(r => r.Item2() == new BigInteger(3))},
    };
    var net = Net(annotations, modularProperties, monolithicProperties);
    NetworkAssert.CheckSoundMonolithic(net);
    NetworkAssert.CheckSound(net);
  }
}
