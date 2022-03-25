using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Zen;

namespace Karesansui.Networks;

// a route which is tagged as external or internal
using TaggedRoute = Pair<BigInteger, bool>;

public class Hijack : Network<Option<TaggedRoute>, Option<TaggedRoute>>
{
  /// <summary>
  /// The symbolic route advertised by the hijacker.
  /// </summary>
  public SymbolicValue<Option<TaggedRoute>> HijackRoute { get; } = new("hijack");

  public static Zen<Option<TaggedRoute>> DestRoute = Option.Create(Pair.Create<BigInteger, bool>(BigInteger.Zero, false));

  /// <summary>
  /// Construct a hijack network: a network containing a legitimate internal destination, and an external node
  /// which can advertise a hijacking route to the network's members.
  /// Routes have a length and an internal/external boolean flag: when the flag is true, the route is external.
  /// All members of the network should prefer internal routes over external ones.
  /// Raises an ArgumentOutOfRangeException if either the destination or hijacker is not present in the given topology.
  /// </summary>
  /// <param name="topology">The given network's topology.</param>
  /// <param name="hijacker">The hijacking external node.</param>
  /// <param name="dest">The internal destination node.</param>
  /// <param name="annotations">The network annotations.</param>
  /// <param name="convergeTime">The time at which the network state converges.</param>
  public Hijack(Topology topology, string hijacker, string dest,
    Func<SymbolicValue<Option<TaggedRoute>>,
      Dictionary<string, Func<Zen<Option<TaggedRoute>>, Zen<BigInteger>, Zen<bool>>>> annotations,
    BigInteger convergeTime)
    : base(topology,
      topology.ForAllEdges(_ => Lang.Omap(Lang.Product(Lang.Incr(1), Lang.Identity<bool>()))),
      Lang.Omap2<TaggedRoute>(Merge),
      new Dictionary<string, Zen<Option<TaggedRoute>>>(),
      new Dictionary<string, Func<Zen<Option<TaggedRoute>>, Zen<BigInteger>, Zen<bool>>>(),
      topology.ForAllNodes(n => Lang.Finally(convergeTime, Property(hijacker, n))),
      topology.ForAllNodes(n => Property(hijacker, n)), Array.Empty<SymbolicValue<Option<TaggedRoute>>>())
  {
    if (!topology.HasNode(hijacker))
    {
      throw new ArgumentOutOfRangeException($"Hijack network does not contain the given hijacker node {hijacker}.");
    }

    if (!topology.HasNode(dest))
    {
      throw new ArgumentOutOfRangeException($"Hijack network does not contain the given destination node {dest}.");
    }

    InitialValues = topology.ForAllNodes(n =>
      n == hijacker ? HijackRoute.Value :
      n == dest ? DestRoute : Option.Null<TaggedRoute>());
    Symbolics = new[] {HijackRoute};
    this.Annotations = annotations(HijackRoute);
    HijackRoute.Constraint = DeriveHijackConstraint();
  }

  private static Func<Zen<Option<TaggedRoute>>, Zen<bool>> DeriveHijackConstraint()
  {
    return maybeRoute => maybeRoute.Case(() => true, pair => pair.Item2());
  }

  /// <summary>
  /// Prefer internal routes over external routes, then prefer shorter routes over longer routes.
  /// </summary>
  /// <param name="r1">The first route.</param>
  /// <param name="r2">The second route.</param>
  /// <returns>The preferred route between the two given.</returns>
  private static Zen<TaggedRoute> Merge(Zen<TaggedRoute> r1, Zen<TaggedRoute> r2)
  {
    return If(r1.Item2(), r2,
      If(r2.Item2(), r1, If(r1.Item1() < r2.Item1(), r1, r2)));
  }

  /// <summary>
  /// Return True if the given route has some value, and that value is not external.
  /// </summary>
  /// <param name="r">A route of the network.</param>
  /// <returns>True if the route is not null and not external.</returns>
  public static Zen<bool> HasInternalRoute(Zen<Option<TaggedRoute>> r)
  {
    return r.Case(
      () => false,
      tr => Not(tr.Item2()));
  }

  private static Func<Zen<Option<TaggedRoute>>, Zen<bool>> Property(string hijacker, string node)
  {
    return hijacker == node ? _ => true : HasInternalRoute;
  }
}
