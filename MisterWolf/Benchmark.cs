using Timepiece;
using ZenLib;

namespace MisterWolf;

public static class Benchmark
{
  internal static Infer<bool> BooleanReachability(Topology topology, Dictionary<string, Zen<bool>> initialValues)
  {
    // initially, the route can be anything
    var beforeInvariants = topology.MapNodes(_ => Lang.True<bool>());
    // eventually, it must be true
    var afterInvariants = topology.MapNodes(_ => Lang.Identity<bool>());

    return new Infer<bool>(topology, topology.MapEdges(_ => Lang.Identity<bool>()), Zen.Or, initialValues,
      beforeInvariants, afterInvariants);
  }

  internal static Infer<bool> StrongBooleanReachability(Topology topology, Dictionary<string, Zen<bool>> initialValues)
  {
    // initially, the route is false
    var beforeInvariants = topology.MapNodes(_ => Lang.Not(Lang.Identity<bool>()));
    // eventually, it must be true
    var afterInvariants = topology.MapNodes(_ => Lang.Identity<bool>());

    return new Infer<bool>(topology, topology.MapEdges(_ => Lang.Identity<bool>()), Zen.Or, initialValues,
      beforeInvariants, afterInvariants);
  }

  internal static Infer<Option<uint>> OintPathLength(Topology topology,
    Dictionary<string, Zen<Option<uint>>> initialValues,
    Dictionary<string, uint> upperBounds)
  {
    var beforeInvariants = topology.MapNodes(_ => Lang.True<Option<uint>>());
    // eventually, the route must be less than the specified max
    var afterInvariants = new Dictionary<string, Func<Zen<Option<uint>>, Zen<bool>>>(upperBounds.Select(b =>
      new KeyValuePair<string, Func<Zen<Option<uint>>, Zen<bool>>>(b.Key, Lang.IfSome<uint>(x => x <= b.Value))));
    return new Infer<Option<uint>>(topology, topology.MapEdges(_ => Lang.Omap<uint, uint>(x => x + 1)),
      Lang.Omap2<uint>(Zen.Min), initialValues, beforeInvariants, afterInvariants);
  }

  internal static Infer<Option<uint>> SingleDestinationOintPathLength(Topology topology, string destination,
    Dictionary<string, uint> upperBounds)
  {
    return OintPathLength(topology, topology.MapNodes(n => n == destination ? Option.Some(0U) : Option.Null<uint>()),
      upperBounds);
  }
}
