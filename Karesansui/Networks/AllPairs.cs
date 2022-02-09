using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui.Networks;

public class AllPairs : ShortestPath<string>
{
  private static readonly SymbolicValue<string> d = new("dnode");

  public AllPairs(Topology topology,
    Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations,
    BigInteger convergeTime) : base(topology,
    topology.ForAllNodes(n => If(d.Equals(n), Some<BigInteger>(BigInteger.Zero), Null<BigInteger>())),
    annotations,
    new[] {d}, convergeTime)
  {
    d.Constraint = DeriveDestConstraint(topology);
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

  private static AllPairs Net(
    Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Default.Path(3);

    var convergeTime = new BigInteger(4);

    return new AllPairs(topology, annotations, convergeTime);
  }

  public static AllPairs Sound()
  {
    Console.WriteLine("Sound annotations:");
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {
        "A",
        Lang.Until(
          If(d == "A", new BigInteger(0),
            If<BigInteger>(d == "B", new BigInteger(1), new BigInteger(2))),
          Lang.IsNone<BigInteger>(), Lang.IsSome<BigInteger>())
      },
      {
        "B",
        Lang.Until(
          If<BigInteger>(d != "B", new BigInteger(1), new BigInteger(0)),
          Lang.IsNone<BigInteger>(), Lang.IsSome<BigInteger>())
      },
      {
        "C",
        Lang.Until(
          If(d == "A", new BigInteger(2),
            If<BigInteger>(d == "B", new BigInteger(1), new BigInteger(0))),
          Lang.IsNone<BigInteger>(), Lang.IsSome<BigInteger>())
      }
    };
    return Net(annotations);
  }

  public static AllPairs Unsound()
  {
    Console.WriteLine("Unsound annotations:");
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Finally(new BigInteger(1), Lang.IsSome<BigInteger>())},
      {"B", Lang.Finally(new BigInteger(1), Lang.IsSome<BigInteger>())},
      {"C", Lang.Finally(new BigInteger(1), Lang.IsSome<BigInteger>())},
    };
    return Net(annotations);
  }
}
