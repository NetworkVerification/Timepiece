using System;
using System.Collections.Generic;
using System.Numerics;
using Karesansui.Networks;
using Xunit;
using ZenLib;

namespace Karesansui.Tests;

using FBagRoute = Pair<int, FBag<string>>;
using FBagAdd = Func<Zen<FBag<string>>, Zen<string>, Zen<FBag<string>>>;
using SetRoute = Pair<int, Set<string>>;
using SetAdd = Func<Zen<Set<string>>, Zen<string>, Zen<Set<string>>>;

public static class TagTests
{
  private static Network<FBagRoute, Unit> BagNet(
    Func<(string, string), Func<Zen<FBagRoute>, Zen<FBagRoute>>> transfer,
    Dictionary<string, Func<Zen<FBagRoute>, Zen<bool>>> monolithicProperties,
    Dictionary<string, Func<Zen<FBagRoute>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Topologies.Path(3);
    var start = Pair.Create(Zen.Constant(0), FBag.Create<string>());
    var initial = new Dictionary<string, Zen<FBagRoute>>
    {
      {"A", start},
      {"B", Pair.Create(Zen.Constant(int.MaxValue), FBag.Create<string>())},
      {"C", Pair.Create(Zen.Constant(int.MaxValue), FBag.Create<string>())}
    };
    var modularProperties = new Dictionary<string, Func<Zen<FBagRoute>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals(start)},
      {"B", Lang.Finally(new BigInteger(1), monolithicProperties["B"])},
      {"C", Lang.Finally(new BigInteger(2), monolithicProperties["C"])}
    };

    return new Network<FBagRoute, Unit>(topology, topology.ForAllEdges(transfer), Merge, initial,
      annotations,
      modularProperties, monolithicProperties, Array.Empty<SymbolicValue<Unit>>());
  }

  private static Network<SetRoute, Unit> SetNet(Dictionary<string, Func<Zen<SetRoute>, Zen<bool>>> monolithicProperties,
    Dictionary<string, Func<Zen<SetRoute>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Topologies.Path(3);
    var start = Pair.Create(Zen.Constant(0), Set.Empty<string>());
    var initial = new Dictionary<string, Zen<SetRoute>>
    {
      {"A", start},
      {"B", Pair.Create(Zen.Constant(int.MaxValue), Set.Empty<string>())},
      {"C", Pair.Create(Zen.Constant(int.MaxValue), Set.Empty<string>())}
    };
    var modularProperties = new Dictionary<string, Func<Zen<SetRoute>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals(start)},
      {"B", Lang.Finally(new BigInteger(1), monolithicProperties["B"])},
      {"C", Lang.Finally(new BigInteger(2), monolithicProperties["C"])}
    };

    return new Network<SetRoute, Unit>(topology, topology.ForAllEdges(SetTransfer), Merge, initial,
      annotations,
      modularProperties, monolithicProperties, Array.Empty<SymbolicValue<Unit>>());
  }

  private static Func<(string, string), Func<Zen<FBagRoute>, Zen<FBagRoute>>> FBagTransferWithBehavior(
    Func<Zen<FBag<string>>, Zen<string>, Zen<FBag<string>>> addBehavior)
  {
    return edge => r => Pair.Create(r.Item1() + 1, addBehavior(r.Item2(), edge.Item1));
  }

  private static Func<Zen<SetRoute>, Zen<SetRoute>> SetTransfer((string, string) edge)
  {
    return r => Pair.Create(r.Item1() + 1, r.Item2().Add(edge.Item1));
  }

  private static Zen<Pair<int, T>> Merge<T>(Zen<Pair<int, T>> r1, Zen<Pair<int, T>> r2)
  {
    return Zen.If(r1.Item1() < r2.Item1(), r1, r2);
  }

  private static void BagNetPassesGoodMonoChecks(FBagAdd addBehavior)
  {
    var monolithicProperties = new Dictionary<string, Func<Zen<FBagRoute>, Zen<bool>>>
    {
      {"A", p => Zen.And(p.Item1() == 0, p.Item2().IsEmpty())},
      {"B", p => Zen.And(p.Item1() == 1, p.Item2().Contains("A"))},
      {"C", p => Zen.And(p.Item1() == 2, p.Item2().Contains("A"), p.Item2().Contains("B"))},
    };
    var net = BagNet(FBagTransferWithBehavior(addBehavior), monolithicProperties,
      new Dictionary<string, Func<Zen<FBagRoute>, Zen<BigInteger>, Zen<bool>>>());
    NetworkAssert.CheckSoundMonolithic(net);
  }

  [Fact]
  public static void BagNetAddPassesGoodMonoChecks()
  {
    BagNetPassesGoodMonoChecks(FBag.Add);
  }

  [Fact]
  public static void BagNetAddIfSpacePassesGoodMonoChecks()
  {
    BagNetPassesGoodMonoChecks(FBag.AddIfSpace);
  }

  private static void BagNetFailsBadMonoChecks(Func<Zen<FBag<string>>, Zen<string>, Zen<FBag<string>>> addBehavior)
  {
    var monolithicProperties = new Dictionary<string, Func<Zen<FBagRoute>, Zen<bool>>>
    {
      {"A", p => p.Item1() == 0},
      {"B", p => p.Item1() == 0},
      {"C", p => p.Item1() == 0},
    };
    var net = BagNet(FBagTransferWithBehavior(addBehavior), monolithicProperties,
      new Dictionary<string, Func<Zen<FBagRoute>, Zen<BigInteger>, Zen<bool>>>());
    NetworkAssert.CheckUnsoundCheck(net, SmtCheck.Monolithic);
  }

  [Fact]
  public static void BagNetAddFailsBadMonoChecks()
  {
    BagNetFailsBadMonoChecks(FBag.Add);
  }

  [Fact]
  public static void BagNetAddIfSpaceFailsBadMonoChecks()
  {
    BagNetFailsBadMonoChecks(FBag.AddIfSpace);
  }

  [Fact]
  public static void SetNetPassesGoodMonoChecks()
  {
    var monolithicProperties = new Dictionary<string, Func<Zen<SetRoute>, Zen<bool>>>
    {
      {"A", p => Zen.And(p.Item1() == 0, p.Item2() == Set.Empty<string>())},
      {"B", p => Zen.And(p.Item1() == 1, p.Item2().Contains("A"))},
      {"C", p => Zen.And(p.Item1() == 2, p.Item2().Contains("A"), p.Item2().Contains("B"))},
    };
    var net = SetNet(monolithicProperties,
      new Dictionary<string, Func<Zen<SetRoute>, Zen<BigInteger>, Zen<bool>>>());
    NetworkAssert.CheckSoundMonolithic(net);
  }

  [Fact]
  public static void SetNetFailsBadMonoChecks()
  {
    var monolithicProperties = new Dictionary<string, Func<Zen<SetRoute>, Zen<bool>>>
    {
      {"A", p => p.Item1() == 0},
      {"B", p => p.Item1() == 0},
      {"C", p => p.Item1() == 0},
    };
    var net = SetNet(monolithicProperties,
      new Dictionary<string, Func<Zen<SetRoute>, Zen<BigInteger>, Zen<bool>>>());
    NetworkAssert.CheckUnsoundCheck(net, SmtCheck.Monolithic);
  }
}
