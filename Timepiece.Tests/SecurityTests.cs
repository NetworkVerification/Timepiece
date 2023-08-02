using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.Datatypes;
using Timepiece.Networks;
using Xunit;
using ZenLib;

namespace Timepiece.Tests;

/// <summary>
/// Tests based on examples presented in Tim Alberdingk Thijm's dissertation, "Modular Control Plane Verification".
/// </summary>
public static class SecurityTests
{
  private const string Tag = "t";

  private static readonly Digraph<string> Digraph = new(new Dictionary<string, List<string>>
  {
    {"a", new List<string> {"a", "b"}},
    {"b", new List<string> {"a", "c", "d"}},
    {"c", new List<string> {"a", "b", "d"}},
    {"d", new List<string> {"b", "c", "e"}},
    {"e", new List<string> {"d"}}
  });

  public static AnnotatedNetwork<Option<Bgp>, string, Option<Bgp>> Net(
    Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<string, Func<Zen<Option<Bgp>>, Zen<bool>>> monolithicProperties)
  {
    var externalRoute = new SymbolicValue<Option<Bgp>>("externalRoute");
    var initialValues = new Dictionary<string, Zen<Option<Bgp>>>
    {
      {"a", Option.Create<Bgp>(new Bgp(100, 0, new CSet<string>(Tag)))},
      {"b", Option.Null<Bgp>()},
      {"c", Option.Null<Bgp>()},
      {"d", Option.Null<Bgp>()},
      {"e", externalRoute.Value},
    };
    return new AnnotatedNetwork<Option<Bgp>, string, Option<Bgp>>(Digraph, Transfer, Lang.Omap2<Bgp>(Bgp.Min),
      initialValues, annotations, modularProperties, monolithicProperties, new[] {externalRoute});
  }

  private static Dictionary<(string, string), Func<Zen<Option<Bgp>>, Zen<Option<Bgp>>>> Transfer =>
    Digraph.MapEdges(e => e switch
    {
      // if the route from a is tagged, we don't send it to c
      ("a", "c") => Lang.Test(Lang.IfSome<Bgp>(b => b.HasTag(Tag)), Lang.Const(Option.None<Bgp>()),
        Lang.Omap<Bgp, Bgp>(BgpExtensions.IncrementAsLength)),
      // d prefers routes from b over routes from c
      ("b", "d") => Lang.Omap<Bgp, Bgp>(b => b.WithLp(new BigInteger(150)).IncrementAsLength()),
      ("c", "d") => Lang.Omap<Bgp, Bgp>(b => b.WithLp(new BigInteger(100)).IncrementAsLength()),
      // drop routes from e to d
      ("e", "d") => Lang.Const(Option.None<Bgp>()),
      _ => Lang.Omap<Bgp, Bgp>(BgpExtensions.IncrementAsLength)
    });

  [Fact]
  public static void DHasTaggedRoute()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"a", Lang.Globally(Lang.IfSome<Bgp>(b => Zen.And(b.LpEquals(new BigInteger(100)), b.HasTag(Tag))))},
      {
        "b",
        Lang.Until(new BigInteger(1), Lang.IsNone<Bgp>(),
          Lang.IfSome<Bgp>(b => Zen.And(b.LpEquals(new BigInteger(100)), b.HasTag(Tag))))
      },
      {
        "c",
        Lang.Until(new BigInteger(2), Lang.IsNone<Bgp>(),
          Lang.IfSome<Bgp>(b => b.LpEquals(new BigInteger(100))))
      },
      {"d", Lang.Until(new BigInteger(2), Lang.IsNone<Bgp>(), Lang.IfSome<Bgp>(b => b.HasTag(Tag)))},
      {"e", Lang.Globally(Lang.True<Option<Bgp>>())},
    };
    var modularProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"a", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"b", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"c", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"d", Lang.Finally(new BigInteger(2), Lang.IfSome<Bgp>(b => b.HasTag(Tag)))},
      {"e", Lang.Globally(Lang.True<Option<Bgp>>())},
    };
    var monolithicProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<bool>>>
    {
      {"a", Lang.True<Option<Bgp>>()},
      {"b", Lang.True<Option<Bgp>>()},
      {"c", Lang.True<Option<Bgp>>()},
      {"d", Lang.IfSome<Bgp>(b => b.HasTag(Tag))},
      {"e", Lang.True<Option<Bgp>>()},
    };
    var net = Net(annotations, modularProperties, monolithicProperties);
    NetworkAssert.CheckSoundMonolithic(net);
    NetworkAssert.CheckSound(net);
  }
}
