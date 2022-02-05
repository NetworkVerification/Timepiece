using System;
using System.Collections.Generic;
using System.Numerics;
using Karesansui.Networks;
using ZenLib;

namespace Karesansui;

public static class Simple
{
  /// <summary>
  ///     Generate a simple example network.
  /// </summary>
  public static Network<Option<BigInteger>, Unit> Net(
    Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    // generates an "A"--"B"--"C" topology
    var topology = Default.Path(3);

    var initialValues = new Dictionary<string, Zen<Option<BigInteger>>>
    {
      {"A", Option.Some(BigInteger.Zero)},
      {"B", Option.None<BigInteger>()},
      {"C", Option.None<BigInteger>()}
    };

    var symbolics = Array.Empty<SymbolicValue<Unit>>();

    return new ShortestPath<Unit>(topology, initialValues, annotations, symbolics, new BigInteger(8));
  }

  public static Network<Option<BigInteger>, Unit> Sound()
  {
    Console.WriteLine("Sound annotations:");
    // sound annotations here. they are overapproximate but sufficient to prove what we want
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals<Option<BigInteger>>(Option.Some(BigInteger.Zero))},
      {
        "B",
        Lang.Until(new BigInteger(1), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r == new BigInteger(1)))
      },
      {
        "C",
        Lang.Until(new BigInteger(2), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r == new BigInteger(2)))
      }
    };
    return Net(annotations);
  }

  public static Network<Option<BigInteger>, Unit> Unsound()
  {
    Console.WriteLine("Unsound annotations:");
    // unsound annotations
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals<Option<BigInteger>>(Option.Some(BigInteger.Zero))},
      {"B", Lang.Never(Lang.IsSome<BigInteger>())},
      {"C", Lang.Never(Lang.IsSome<BigInteger>())}
    };
    return Net(annotations);
  }
}