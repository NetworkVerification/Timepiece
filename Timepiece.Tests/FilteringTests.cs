using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.DataTypes;
using Timepiece.Networks;
using Xunit;
using ZenLib;
using Array = System.Array;

namespace Timepiece.Tests;

/// <summary>
///   Tests based on the examples presented in "Modular Control Plane Verification via Temporal Invariants"
///   by Alberdingk Thijm et al. from POPL 2023.
/// </summary>
public static class FilteringTests
{
  private const string Tag = "t";

  private static readonly Digraph<string> Digraph = new(new Dictionary<string, List<string>>
  {
    {"n", new List<string>()},
    {"w", new List<string>()},
    {"v", new List<string> {"n", "w", "d"}},
    {"d", new List<string> {"v"}},
    {"e", new List<string> {"d"}}
  });

  private static AnnotatedNetwork<Option<Bgp>, string> Net(
    Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<string, Func<Zen<Option<Bgp>>, Zen<bool>>> monolithicProperties)
  {
    var initialValues = new Dictionary<string, Zen<Option<Bgp>>>
    {
      {"n", Option.None<Bgp>()},
      {"w", Option.Create<Bgp>(new Bgp(100, BigInteger.Zero, new CSet<string>()))},
      {"v", Option.None<Bgp>()},
      {"d", Option.None<Bgp>()},
      {"e", Option.None<Bgp>()}
    };
    return new AnnotatedNetwork<Option<Bgp>, string>(Digraph, Transfer(), Lang.Omap2<Bgp>(Bgp.Min),
      initialValues,
      annotations, modularProperties, monolithicProperties, Array.Empty<ISymbolic>());
  }

  private static AnnotatedNetwork<Pair<Option<Bgp>, bool>, string> NetGhostState(
    Dictionary<string, Func<Zen<Pair<Option<Bgp>, bool>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<Pair<Option<Bgp>, bool>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<string, Func<Zen<Pair<Option<Bgp>, bool>>, Zen<bool>>> monolithicProperties)
  {
    var initialValues = new Dictionary<string, Zen<Pair<Option<Bgp>, bool>>>
    {
      {"n", Pair.Create<Option<Bgp>, bool>(Option.None<Bgp>(), Zen.False())},
      {"w", Pair.Create(Option.Create<Bgp>(new Bgp()), Zen.True())},
      {"v", Pair.Create<Option<Bgp>, bool>(Option.None<Bgp>(), Zen.False())},
      {"d", Pair.Create<Option<Bgp>, bool>(Option.None<Bgp>(), Zen.False())},
      {"e", Pair.Create<Option<Bgp>, bool>(Option.None<Bgp>(), Zen.False())}
    };
    var transfer = Transfer();
    return new AnnotatedNetwork<Pair<Option<Bgp>, bool>, string>(Digraph,
      Digraph.MapEdges(e => Lang.Product(transfer[e], Lang.Identity<bool>())),
      Lang.MergeBy<Pair<Option<Bgp>, bool>, Option<Bgp>>(Lang.Omap2<Bgp>(Bgp.Min), p => p.Item1()),
      initialValues,
      annotations, modularProperties, monolithicProperties, Array.Empty<ISymbolic>());
  }

  private static Dictionary<(string, string), Func<Zen<Option<Bgp>>, Zen<Option<Bgp>>>> Transfer()
  {
    // add the tag on nv and wv, drop if tagged on de
    return Digraph.MapEdges(e => e switch
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
      {"v", Lang.Globally(Lang.OrSome<Bgp>(b => b.HasTag(Tag)))}
    };
    var modularProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"e", Lang.Globally(Lang.OrSome<Bgp>(b => b.HasTag(Tag)))},
      {"n", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"w", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"d", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"v", Lang.Globally(Lang.True<Option<Bgp>>())}
    };
    var monolithicProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<bool>>>
    {
      {"e", Lang.OrSome<Bgp>(b => b.HasTag(Tag))},
      {"n", Lang.True<Option<Bgp>>()},
      {"w", Lang.True<Option<Bgp>>()},
      {"d", Lang.True<Option<Bgp>>()},
      {"v", Lang.True<Option<Bgp>>()}
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
      {"v", Lang.Until(new BigInteger(1), Lang.IsNone<Bgp>(), Lang.IfSome<Bgp>(b => b.HasTag(Tag)))},
      {"d", Lang.Until(new BigInteger(2), Lang.IsNone<Bgp>(), Lang.IfSome<Bgp>(b => b.HasTag(Tag)))},
      {"e", Lang.Finally(new BigInteger(3), Lang.IsSome<Bgp>())}
    };
    var modularProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"n", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"w", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"v", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"d", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"e", Lang.Finally(new BigInteger(3), Lang.IsSome<Bgp>())}
    };
    var monolithicProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<bool>>>
    {
      {"n", Lang.True<Option<Bgp>>()},
      {"w", Lang.True<Option<Bgp>>()},
      {"v", Lang.True<Option<Bgp>>()},
      {"d", Lang.True<Option<Bgp>>()},
      {"e", Lang.IsSome<Bgp>()}
    };
    var net = Net(annotations, modularProperties, monolithicProperties);
    // NetworkAssert.CheckUnsoundCheck(net, SmtCheck.Inductive);
    NetworkAssert.CheckSound(net);
  }

  [Fact]
  public static void BadAnnotationsFailToMakeEUnreachable()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"n", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"w", Lang.Globally(Lang.IfSome<Bgp>(b => b.GetLp() == new BigInteger(100)))},
      {"v", Lang.Globally(Lang.OrSome<Bgp>(b => Zen.And(Zen.Not(b.HasTag(Tag)), b.GetLp() == new BigInteger(200))))},
      {"d", Lang.Globally(Lang.OrSome<Bgp>(b => Zen.And(Zen.Not(b.HasTag(Tag)), b.GetLp() == new BigInteger(200))))},
      {"e", Lang.Globally(Lang.IsNone<Bgp>())}
    };
    var modularProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"n", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"w", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"v", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"d", Lang.Globally(Lang.True<Option<Bgp>>())},
      {"e", Lang.Globally(Lang.IsNone<Bgp>())}
    };
    var monolithicProperties = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<bool>>>
    {
      {"n", Lang.True<Option<Bgp>>()},
      {"w", Lang.True<Option<Bgp>>()},
      {"v", Lang.True<Option<Bgp>>()},
      {"d", Lang.True<Option<Bgp>>()},
      {"e", Lang.IsNone<Bgp>()}
    };
    var net = Net(annotations, modularProperties, monolithicProperties);
    NetworkAssert.CheckUnsoundCheck(net, SmtCheck.Inductive);
  }

  [Fact]
  public static void EReachableFromWGhostState()
  {
    var annotations = new Dictionary<string, Func<Zen<Pair<Option<Bgp>, bool>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"n", Lang.Globally(Lang.Second<Option<Bgp>, bool>(Zen.Not))},
      {"w", Lang.Globally(Lang.Both(Lang.IfSome<Bgp>(b => b.GetLp() == new BigInteger(100)), Lang.Identity<bool>()))},
      {
        "v",
        Lang.Until(new BigInteger(1), Lang.First<Option<Bgp>, bool>(Lang.IsNone<Bgp>()),
          Lang.Both(Lang.IfSome<Bgp>(b => b.HasTag(Tag)), Lang.Identity<bool>()))
      },
      {
        "d",
        Lang.Until(new BigInteger(2), Lang.First<Option<Bgp>, bool>(Lang.IsNone<Bgp>()),
          Lang.Both(Lang.IfSome<Bgp>(b => b.HasTag(Tag)), Lang.Identity<bool>()))
      },
      {"e", Lang.Finally(new BigInteger(3), Lang.Both(Lang.IsSome<Bgp>(), Lang.Identity<bool>()))}
    };
    var modularProperties = new Dictionary<string, Func<Zen<Pair<Option<Bgp>, bool>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"n", Lang.Globally(Lang.True<Pair<Option<Bgp>, bool>>())},
      {"w", Lang.Globally(Lang.True<Pair<Option<Bgp>, bool>>())},
      {"v", Lang.Globally(Lang.True<Pair<Option<Bgp>, bool>>())},
      {"d", Lang.Globally(Lang.True<Pair<Option<Bgp>, bool>>())},
      {"e", Lang.Finally(new BigInteger(3), Lang.Both(Lang.IsSome<Bgp>(), Lang.Identity<bool>()))}
    };
    var monolithicProperties = new Dictionary<string, Func<Zen<Pair<Option<Bgp>, bool>>, Zen<bool>>>
    {
      {"n", Lang.True<Pair<Option<Bgp>, bool>>()},
      {"w", Lang.True<Pair<Option<Bgp>, bool>>()},
      {"v", Lang.True<Pair<Option<Bgp>, bool>>()},
      {"d", Lang.True<Pair<Option<Bgp>, bool>>()},
      {"e", Lang.Both(Lang.IsSome<Bgp>(), Lang.Identity<bool>())}
    };
    var net = NetGhostState(annotations, modularProperties, monolithicProperties);
    NetworkAssert.CheckSound(net);
  }
}
