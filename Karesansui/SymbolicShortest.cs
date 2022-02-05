using System;
using System.Collections.Generic;
using System.Numerics;
using Karesansui.Networks;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui;

public class Symbolic
{
  private static readonly SymbolicValue<BigInteger> d = new("d", r => r >= BigInteger.Zero);

  public static ShortestPath<BigInteger> Net(
    Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    var topology = Default.Complete(3);

    var initialValues = new Dictionary<string, Zen<Option<BigInteger>>>
    {
      {"A", Some(d.Value)},
      {"B", Option.None<BigInteger>()},
      {"C", Option.None<BigInteger>()}
    };

    var symbolics = new[] {d};

    return new ShortestPath<BigInteger>(topology, initialValues, annotations, symbolics, 2);
  }

  public static Network<Option<BigInteger>, BigInteger> Sound()
  {
    Console.WriteLine("Sound annotations:");
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals(Some(d.Value))},
      {"B", Lang.Until(new BigInteger(1), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r >= d.Value))},
      {"C", Lang.Until(new BigInteger(1), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r >= d.Value))}
    };

    return Net(annotations);
  }

  public static Network<Option<BigInteger>, BigInteger> Unsound()
  {
    Console.WriteLine("Unsound annotations:");
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Globally(Lang.IfSome<BigInteger>(r => r <= d.Value))},
      {"B", Lang.Finally(new BigInteger(1), Lang.IfSome<BigInteger>(r => r <= d.Value))},
      {"C", Lang.Finally(new BigInteger(1), Lang.IfSome<BigInteger>(r => r <= d.Value))}
    };

    return Net(annotations);
  }
}