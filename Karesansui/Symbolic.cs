using System;
using System.Collections.Generic;
using System.Numerics;
using Karesansui.Networks;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui;

public static class Symbolic
{
    private static readonly Zen<BigInteger> d = Symbolic<BigInteger>();

    public static Network<Option<BigInteger>, BigInteger> Net(
        Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations)
    {
        var topology = Default.Complete(3);

        var initialValues = new Dictionary<string, Zen<Option<BigInteger>>>
        {
            {"A", Some(d)},
            {"B", Option.None<BigInteger>()},
            {"C", Option.None<BigInteger>()}
        };

        var symbolics = new Dictionary<Zen<BigInteger>, Func<Zen<BigInteger>, Zen<bool>>>
        {
            {d, _ => d > new BigInteger(0)}
        };

        return new ShortestPath<BigInteger>(topology, initialValues, annotations, symbolics, 2);
    }

    public static Network<Option<BigInteger>, BigInteger> Sound()
    {
        Console.WriteLine("Sound annotations:");
        var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
        {
            {"A", Lang.Equals(Some(d))},
            {"B", Lang.Until(new BigInteger(1), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r >= d))},
            {"C", Lang.Until(new BigInteger(1), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r >= d))}
        };

        return Net(annotations);
    }

    public static Network<Option<BigInteger>, BigInteger> Unsound()
    {
        Console.WriteLine("Unsound annotations:");
        var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
        {
            {"A", Lang.Globally(Lang.IfSome<BigInteger>(r => r <= d))},
            {"B", Lang.Finally(new BigInteger(1), Lang.IfSome<BigInteger>(r => r <= d))},
            {"C", Lang.Finally(new BigInteger(1), Lang.IfSome<BigInteger>(r => r <= d))}
        };

        return Net(annotations);
    }
}