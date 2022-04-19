using System;
using System.Collections.Generic;
using System.Numerics;
using Karesansui.Datatypes;
using Karesansui.Networks;
using Xunit;
using ZenLib;

namespace Karesansui.Tests;

public static class FilteringTests
{
  private const string Tag = "t";

  public static Network<Option<Bgp>, Unit> Net(
    Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<string, Func<Zen<Option<Bgp>>, Zen<bool>>> monolithicProperties)
  {
    var topology = new Topology(new Dictionary<string, List<string>>
    {
      {"n", new List<string>()},
      {"w", new List<string>()},
      {"v", new List<string> {"n", "w"}},
      {"d", new List<string> {"v"}},
      {"e", new List<string> {"d"}}
    });
    var initialValues = new Dictionary<string, Zen<Option<Bgp>>>
    {
      {"n", Option.None<Bgp>()},
      {"w", Option.Create<Bgp>(new Bgp(100, BigInteger.Zero, new Set<string>()))},
      {"v", Option.None<Bgp>()},
      {"d", Option.None<Bgp>()},
      {"e", Option.None<Bgp>()},
    };
    return new Network<Option<Bgp>, Unit>(topology, Transfer(topology), Lang.Omap2<Bgp>(BgpExtensions.Min),
      initialValues,
      annotations, modularProperties, monolithicProperties, Array.Empty<SymbolicValue<Unit>>());
  }

  private static Dictionary<(string, string), Func<Zen<Option<Bgp>>, Zen<Option<Bgp>>>> Transfer(Topology topology)
  {
    // add the tag on nv and wv, drop if tagged on de
    return topology.ForAllEdges(e => e switch
    {
      ("w", "v") => Lang.Omap<Bgp, Bgp>(b => b.IncrementAsLength().AddTag(Tag)),
      ("n", "v") => _ => Option.None<Bgp>(),
      ("d", "e") => Lang.Bind<Bgp, Bgp>(b =>
        Zen.If(b.HasTag(Tag), Option.Create(b.IncrementAsLength()), Option.None<Bgp>())),
      _ => Lang.Omap<Bgp, Bgp>(BgpExtensions.IncrementAsLength)
    });
  }

  [Fact]
  public static void EHasInternalRouteIfReachableFromW()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"e", Lang.Globally(Lang.OrSome<Bgp>(b => b.HasTag(Tag)))},
      {"n", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"w", Lang.Globally(Lang.IfSome<Bgp>(b => b.GetLp() == new BigInteger(100)))},
      {"d", Lang.Globally(Lang.OrSome<Bgp>(b => b.HasTag(Tag)))},
      {"v", Lang.Globally(Lang.OrSome<Bgp>(b => b.HasTag(Tag)))},
    };
    var modularProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"e", Lang.Globally(Lang.OrSome<Bgp>(b => b.HasTag(Tag)))},
      {"n", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"w", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"d", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"v", Lang.Globally(Lang.True<Option<Bgp>>())},
    };
    var monolithicProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<bool>>>
    {
      {"e", Lang.OrSome<Bgp>(b => b.HasTag(Tag))},
      {"n", Lang.True<Option<Bgp>>()},
      {"w", Lang.True<Option<Bgp>>()},
      {"d", Lang.True<Option<Bgp>>()},
      {"v", Lang.True<Option<Bgp>>()},
    };
    var net = Net(annotations, modularProperties, monolithicProperties);
    NetworkAssert.CheckSound(net);
  }

  [Fact]
  public static void EReachableFromW()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"n", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"w", Lang.Globally(Lang.IfSome<Bgp>(b => b.GetLp() == new BigInteger(100)))},
      {"v", Lang.Finally(new BigInteger(1),Lang.IfSome<Bgp>(b => b.HasTag(Tag)))},
      {"d", Lang.Finally(new BigInteger(2), Lang.IfSome<Bgp>(b => b.HasTag(Tag)))},
      {"e", Lang.Finally(new BigInteger(3), Lang.IsSome<Bgp>())},
    };
    var modularProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"n", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"w", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"v", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"d", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"e", Lang.Finally(new BigInteger(3), Lang.IsSome<Bgp>())},
    };
    var monolithicProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<bool>>>
    {
      {"n", Lang.True<Option<Bgp>>()},
      {"w", Lang.True<Option<Bgp>>()},
      {"v", Lang.True<Option<Bgp>>()},
      {"d", Lang.True<Option<Bgp>>()},
      {"e", Lang.IsSome<Bgp>()},
    };
    var net = Net(annotations, modularProperties, monolithicProperties);
    NetworkAssert.CheckSound(net);
  }
}
