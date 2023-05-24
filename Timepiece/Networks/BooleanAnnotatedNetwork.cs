using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;

namespace Timepiece.Networks;

/// <summary>
///   A network with a boolean routing algebra.
/// </summary>
public class BooleanAnnotatedNetwork<TS> : AnnotatedNetwork<bool, TS>
{
  public BooleanAnnotatedNetwork(Network<bool, TS> net,
    Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations,
    BigInteger convergeTime)
    : base(net,
      annotations,
      net.Topology.MapNodes(_ => Lang.Finally(convergeTime, Lang.Identity<bool>())),
      net.Topology.MapNodes(_ => Lang.Identity<bool>()))
  {
  }
}
