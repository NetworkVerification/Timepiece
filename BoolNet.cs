using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace ZenDemo;

/// <summary>
///   A network with a boolean routing algebra.
/// </summary>
public class BoolNet : Network<bool>
{
  public BoolNet(Topology topology,
    Dictionary<string, Zen<bool>> initialValues,
    Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations,
    BigInteger convergeTime)
    : base(topology, topology.ForAllEdges(_ => Lang.Identity<bool>()),
      Or, initialValues, annotations,
      topology.ForAllNodes(_ => Lang.Finally(convergeTime, Lang.Identity<bool>())),
      topology.ForAllNodes(_ => Lang.Identity<bool>()),
      new Dictionary<Zen<object>, Zen<bool>>())
  {
  }

  public static BoolNet Sound()
  {
    Console.WriteLine("Sound annotations:");
    var topology = Default.Path(2);

    var initialValues = topology.ForAllNodes(n => Eq<string>(n, "A"));
    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", (r, _) => r},
      {"B", Lang.Finally(new BigInteger(1), Lang.Identity<bool>())}
    };
    return new BoolNet(topology, initialValues, annotations, new BigInteger(5));
  }
}