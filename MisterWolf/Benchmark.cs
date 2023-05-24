using Timepiece;
using ZenLib;

namespace MisterWolf;

public static class Benchmark
{
  internal static Infer<Option<uint>, Unit> OptionUintPathLength(Topology topology,
    Initialization<Option<uint>> init,
    Dictionary<string, uint> upperBounds)
  {
    var beforeInvariants = topology.MapNodes(_ => Lang.True<Option<uint>>());
    // eventually, the route must be less than the specified max
    var afterInvariants = new Dictionary<string, Func<Zen<Option<uint>>, Zen<bool>>>(upperBounds.Select(b =>
      new KeyValuePair<string, Func<Zen<Option<uint>>, Zen<bool>>>(b.Key, Lang.IfSome<uint>(x => x <= b.Value))));
    return new Infer<Option<uint>, Unit>(topology, topology.MapEdges(_ => Lang.Omap<uint, uint>(x => x + 1)),
      Lang.Omap2<uint>(Zen.Min), init.InitialValues, beforeInvariants, afterInvariants, new SymbolicValue<Unit>[] { });
  }

  internal static Infer<Option<uint>, Unit> SingleDestinationOptionUintPathLength(Topology topology, string destination,
    Dictionary<string, uint> upperBounds)
  {
    return OptionUintPathLength(topology,
      new Initialization<Option<uint>>(topology, destination, Option.Some(0U), Option.None<uint>()),
      upperBounds);
  }
}
