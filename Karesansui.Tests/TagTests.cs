using System;
using System.Collections.Generic;
using System.Numerics;
using Karesansui.Networks;
using Xunit;
using ZenLib;

namespace Karesansui.Tests;

public static class TagTests
{
  public static Network<Pair<int, FBag<string>>, Unit> BagNet(
    Func<(string, string), Func<Zen<Pair<int, FBag<string>>>, Zen<Pair<int, FBag<string>>>>> transfer,
    Dictionary<string, Func<Zen<Pair<int, FBag<string>>>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Topologies.Path(3);
    var start = Pair.Create(Zen.Constant(0), FBag.Create<string>());
    var initial = new Dictionary<string, Zen<Pair<int, FBag<string>>>>
    {
      {"A", start},
      {"B", Pair.Create(Zen.Constant(int.MaxValue), FBag.Create<string>())},
      {"C", Pair.Create(Zen.Constant(int.MaxValue), FBag.Create<string>())}
    };
    var monolithicProperties = new Dictionary<string, Func<Zen<Pair<int, FBag<string>>>, Zen<bool>>>
    {
      {"A", p => Zen.And(p.Item1() == 0, p.Item2().IsEmpty())},
      {"B", p => Zen.And(p.Item1() == 1, p.Item2().Contains("A"))},
      {"C", p => Zen.And(p.Item1() == 2, p.Item2().Contains("B"), p.Item2().Contains("C"))},
    };
    var modularProperties = new Dictionary<string, Func<Zen<Pair<int, FBag<string>>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals(start)},
      {"B", Lang.Finally(new BigInteger(1), monolithicProperties["B"])},
      {"C", Lang.Finally(new BigInteger(2), monolithicProperties["C"])}
    };

    return new Network<Pair<int, FBag<string>>, Unit>(topology, topology.ForAllEdges(e => transfer(e)), Merge, initial,
      annotations,
      modularProperties, monolithicProperties, Array.Empty<SymbolicValue<Unit>>());
  }

  private static Func<Zen<Pair<int, FBag<string>>>, Zen<Pair<int, FBag<string>>>> TransferAdd((string, string) edge)
  {
    return r => Pair.Create(r.Item1() + 1, r.Item2().Add(edge.Item1));
  }

  private static Func<Zen<Pair<int, FBag<string>>>, Zen<Pair<int, FBag<string>>>> TransferAddIfSpace(
    (string, string) edge)
  {
    return r => Pair.Create(r.Item1() + 1, r.Item2().AddIfSpace(edge.Item1));
  }

  private static Zen<Pair<int, T>> Merge<T>(Zen<Pair<int, T>> r1, Zen<Pair<int, T>> r2)
  {
    return Zen.If(r1.Item1() < r2.Item1(), r1, r2);
  }

  [Fact]
  public static void BagNetAddPassesMonoChecks()
  {
    var net = BagNet(TransferAdd,
      new Dictionary<string, Func<Zen<Pair<int, FBag<string>>>, Zen<BigInteger>, Zen<bool>>>());
    NetworkAssert.CheckSoundMonolithic(net);
  }

  [Fact]
  public static void BagNetAddIfSpacePassesMonoChecks()
  {
    var net = BagNet(TransferAddIfSpace,
      new Dictionary<string, Func<Zen<Pair<int, FBag<string>>>, Zen<BigInteger>, Zen<bool>>>());
    NetworkAssert.CheckSoundMonolithic(net);
  }
}
