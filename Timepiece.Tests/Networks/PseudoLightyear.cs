using System;
using System.Collections.Generic;
using System.Numerics;
using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Tests.Networks;

/// A network replicating the behavior of Lightyear's formal model for checking safety properties.
/// See Tang et al., "Lightyear: Using Modularity to Scale BGP Control Plane Verification" (SIGCOMM 2023).
public class PseudoLightyear<TV, TS> : BgpAnnotatedNetwork<TV, TS> where TV : notnull
{
  public PseudoLightyear(Digraph<TV> digraph, Dictionary<TV, Zen<Option<Bgp>>> initialValues,
    Dictionary<TV, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<TV, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<TV, Func<Zen<Option<Bgp>>, Zen<bool>>> monolithicProperties, SymbolicValue<TS>[] symbolics) : base(
    digraph, initialValues, annotations, modularProperties, monolithicProperties, symbolics)
  {
  }
}
