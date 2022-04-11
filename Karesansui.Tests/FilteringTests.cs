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
    const string tag = "t";
    // add the tag on nv and wv, drop if tagged on de
    return topology.ForAllEdges(e => e switch
    {
      ("n", "v") or ("w", "v") => Lang.Omap<Bgp, Bgp>(b => b.IncrementAsLength().AddTag(tag)),
      ("d", "e") => Lang.Bind<Bgp, Bgp>(b =>
        Zen.If(b.HasTag(tag), Option.None<Bgp>(), Option.Create(b.IncrementAsLength()))),
      _ => Lang.Omap<Bgp, Bgp>(BgpExtensions.IncrementAsLength)
    });
  }

  [Fact]
  public static void EUnreachableFromW()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"e", Lang.Globally(Lang.IsNone<Bgp>())},
      {"n", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"w", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"d", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"v", Lang.Globally(Lang.True<Option<Bgp>>())},
    };
    var modularProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"e", Lang.Globally(Lang.IsNone<Bgp>())},
      {"n", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"w", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"d", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"v", Lang.Globally(Lang.True<Option<Bgp>>())},
    };
    var monolithicProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<bool>>>
    {
      {"e", Lang.IsNone<Bgp>()},
      {"n", Lang.True<Option<Bgp>>()},
      {"w", Lang.True<Option<Bgp>>()},
      {"d", Lang.True<Option<Bgp>>()},
      {"v", Lang.True<Option<Bgp>>()},
    };
    var net = Net(annotations, modularProperties, monolithicProperties);
    NetworkAssert.CheckSound(net);
  }
}
