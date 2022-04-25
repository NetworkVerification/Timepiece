using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Zen;

namespace Timekeeper.Networks;

public class AllPairs : ShortestPath<string>
{
  /// <summary>
  /// The symbolic value representing the destination.
  /// </summary>
  private SymbolicValue<string> D { get; } = new("dnode");

  public AllPairs(Topology topology,
    Func<SymbolicValue<string>, Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>>
      annotations,
    BigInteger convergeTime, Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<bool>>> safetyProperties) : base(
    topology,
    new Dictionary<string, Zen<Option<BigInteger>>>(),
    new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>(),
    Array.Empty<SymbolicValue<string>>(), convergeTime, safetyProperties)
  {
    InitialValues =
      topology.ForAllNodes(n =>
        If(D.EqualsValue(n), Option.Create<BigInteger>(BigInteger.Zero), Option.Null<BigInteger>()));
    Symbolics = new[] {D};
    Annotations = annotations(D);
    D.Constraint = DeriveDestConstraint(topology);
  }

  /// <summary>
  /// Check that some node in the topology is the same as the given string.
  /// </summary>
  /// <param name="topology">The given topology.</param>
  /// <returns>A constraint from string to bool.</returns>
  private static Func<Zen<string>, Zen<bool>> DeriveDestConstraint(Topology topology)
  {
    return s => topology.FoldNodes(False(), (b, n) => Or(b, s == Constant(n)));
  }
}
