using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.Networks;
using Xunit;
using ZenLib;
using Array = System.Array;

namespace Timepiece.Tests;

using CSetRoute = Pair<int, CSet<string>>;
using CSetAdd = Func<Zen<CSet<string>>, string, Zen<CSet<string>>>;
using SetRoute = Pair<int, Set<string>>;
using SetAdd = Func<Zen<Set<string>>, Zen<string>, Zen<Set<string>>>;

public static class TagTests
{
  private static AnnotatedNetwork<CSetRoute, string> CSetNet(
    Func<(string, string), Func<Zen<CSetRoute>, Zen<CSetRoute>>> transfer,
    Dictionary<string, Func<Zen<CSetRoute>, Zen<bool>>> monolithicProperties,
    Dictionary<string, Func<Zen<CSetRoute>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Topologies.Path(3);
    var start = Pair.Create(Zen.Constant(0), CSet.Empty<string>());
    var initial = new Dictionary<string, Zen<CSetRoute>>
    {
      {"A", start},
      {"B", Pair.Create(Zen.Constant(int.MaxValue), CSet.Empty<string>())},
      {"C", Pair.Create(Zen.Constant(int.MaxValue), CSet.Empty<string>())}
    };
    var modularProperties = new Dictionary<string, Func<Zen<CSetRoute>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals(start)},
      {"B", Lang.Finally(new BigInteger(1), monolithicProperties["B"])},
      {"C", Lang.Finally(new BigInteger(2), monolithicProperties["C"])}
    };

    return new AnnotatedNetwork<CSetRoute, string>(topology, topology.MapEdges(transfer), Merge, initial,
      annotations,
      modularProperties, monolithicProperties, Array.Empty<ISymbolic>());
  }

  private static AnnotatedNetwork<SetRoute, string> SetNet(
    Dictionary<string, Func<Zen<SetRoute>, Zen<bool>>> monolithicProperties,
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

    return new AnnotatedNetwork<SetRoute, string>(topology, topology.MapEdges(SetTransfer), Merge, initial,
      annotations,
      modularProperties, monolithicProperties, Array.Empty<ISymbolic>());
  }

  private static Func<(string, string), Func<Zen<CSetRoute>, Zen<CSetRoute>>> CSetTransferWithBehavior(
    CSetAdd addBehavior)
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

  private static void CSetNetPassesGoodMonoChecks(CSetAdd addBehavior)
  {
    var monolithicProperties = new Dictionary<string, Func<Zen<CSetRoute>, Zen<bool>>>
    {
      {"A", p => Zen.And(p.Item1() == 0, p.Item2() == CSet.Empty<string>())},
      {"B", p => Zen.And(p.Item1() == 1, p.Item2().Contains("A"))},
      {"C", p => Zen.And(p.Item1() == 2, p.Item2().Contains("A"), p.Item2().Contains("B"))}
    };
    var net = CSetNet(CSetTransferWithBehavior(addBehavior), monolithicProperties,
      new Dictionary<string, Func<Zen<CSetRoute>, Zen<BigInteger>, Zen<bool>>>());
    NetworkAssert.CheckSoundMonolithic(net);
  }

  [Fact]
  public static void CSetNetAddPassesGoodMonoChecks()
  {
    CSetNetPassesGoodMonoChecks(CSet.Add);
  }

  private static void CSetNetFailsBadMonoChecks(CSetAdd addBehavior)
  {
    var monolithicProperties = new Dictionary<string, Func<Zen<CSetRoute>, Zen<bool>>>
    {
      {"A", p => p.Item1() == 0},
      {"B", p => p.Item1() == 0},
      {"C", p => p.Item1() == 0}
    };
    var net = CSetNet(CSetTransferWithBehavior(addBehavior), monolithicProperties,
      new Dictionary<string, Func<Zen<CSetRoute>, Zen<BigInteger>, Zen<bool>>>());
    NetworkAssert.CheckUnsoundCheck(net, SmtCheck.Monolithic);
  }

  [Fact]
  public static void CSetNetAddFailsBadMonoChecks()
  {
    CSetNetFailsBadMonoChecks(CSet.Add);
  }

  [Fact]
  public static void SetNetPassesGoodMonoChecks()
  {
    var monolithicProperties = new Dictionary<string, Func<Zen<SetRoute>, Zen<bool>>>
    {
      {"A", p => Zen.And(p.Item1() == 0, p.Item2() == Set.Empty<string>())},
      {"B", p => Zen.And(p.Item1() == 1, p.Item2().Contains("A"))},
      {"C", p => Zen.And(p.Item1() == 2, p.Item2().Contains("A"), p.Item2().Contains("B"))}
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
      {"C", p => p.Item1() == 0}
    };
    var net = SetNet(monolithicProperties,
      new Dictionary<string, Func<Zen<SetRoute>, Zen<BigInteger>, Zen<bool>>>());
    NetworkAssert.CheckUnsoundCheck(net, SmtCheck.Monolithic);
  }
}
