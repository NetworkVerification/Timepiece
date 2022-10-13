using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Networks;

/// <summary>
///     A network with a boolean routing algebra.
/// </summary>
public class BooleanNetwork<TS> : Network<bool, TS>
{
  public BooleanNetwork(Topology topology,
    Dictionary<string, Zen<bool>> initialValues,
    Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations,
    SymbolicValue<TS>[] symbolics,
    BigInteger convergeTime)
    : base(topology, topology.MapEdges(_ => Lang.Identity<bool>()),
      Or, initialValues, annotations,
      topology.MapNodes(_ => Lang.Finally(convergeTime, Lang.Identity<bool>())),
      topology.MapNodes(_ => Lang.Identity<bool>()),
      symbolics)
  {
  }
}