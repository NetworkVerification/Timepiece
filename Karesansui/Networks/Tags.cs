using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;
using static ZenLib.Language;

namespace Karesansui.Networks;

public record struct Bgp
{
    public Bgp(BigInteger cost, IList<string> tags)
    {
        Cost = cost;
        Tags = tags;
    }

    public Bgp()
    {
        Cost = default;
        Tags = default;
    }

    /// <summary>
    ///     Abstract cost of the given BGP announcement.
    /// </summary>
    public BigInteger Cost { get; set; }

    /// <summary>
    ///     List of community tags.
    /// </summary>
    public IList<string> Tags { get; set; }

    public override string ToString()
    {
        var tagVal = string.Empty;
        foreach (var tag in Tags)
            if (string.IsNullOrEmpty(tagVal))
                tagVal += $"{tag}";
            else
                tagVal += $", {tag}";
        return $"Bgp(Cost={Cost},Tags=[{tagVal}])";
    }
}

public static class BgpExtensions
{
    public static Zen<BigInteger> GetCost(this Zen<Bgp> b)
    {
        return b.GetField<Bgp, BigInteger>("Cost");
    }

    public static Zen<IList<string>> GetTags(this Zen<Bgp> b)
    {
        return b.GetField<Bgp, IList<string>>("Tags");
    }

    public static Zen<Bgp> SetCost(this Zen<Bgp> b, Zen<BigInteger> cost)
    {
        return b.WithField("Cost", cost);
    }

    public static Zen<Bgp> SetTags(this Zen<Bgp> b, Zen<IList<string>> tags)
    {
        return b.WithField("Tags", tags);
    }

    public static Zen<bool> HasTag(this Zen<Bgp> b, string tag)
    {
        return b.GetTags().Contains(tag);
    }

    public static Zen<Bgp> AddTag(this Zen<Bgp> b, string tag)
    {
        return b.SetTags(b.GetTags().AddFront(tag));
    }
}

public class Tags : Network<Option<Bgp>, Bgp>
{
    public Tags(Topology topology,
        Dictionary<string, Zen<Option<Bgp>>> initialValues,
        Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>> annotations,
        BigInteger convergeTime,
        Dictionary<Zen<Bgp>, Func<Zen<Bgp>, Zen<bool>>> symbolics) : base(topology,
        topology.ForAllEdges(e => Lang.Bind(Transfer(e))), Lang.Omap2<Bgp>(Merge),
        initialValues, annotations, topology.ForAllNodes(_ => Lang.Finally(convergeTime, Lang.IsSome<Bgp>())),
        topology.ForAllNodes(_ => Lang.IsSome<Bgp>()), symbolics)
    {
    }

    public static Tags Sound()
    {
        Console.WriteLine("Sound annotations:");
        var topology = Default.Path(3);

        var start = Some<Bgp>(new Bgp(1, new List<string>()));
        var initialValues = new Dictionary<string, Zen<Option<Bgp>>>
        {
            {"A", start},
            {"B", Option.None<Bgp>()},
            {"C", Option.None<Bgp>()}
        };

        var annotations = new Dictionary<string, Func<Zen<Option<Bgp>>, Zen<BigInteger>, Zen<bool>>>
        {
            {"A", Lang.Equals(start)},
            {
                "B",
                Lang.Until(new BigInteger(1), Lang.IsNone<Bgp>(),
                    Lang.IfSome<Bgp>(b => And(b.GetCost() == new BigInteger(2), Not(b.HasTag("C")))))
            },
            {
                "C",
                Lang.Until(new BigInteger(2), Lang.IsNone<Bgp>(),
                    Lang.IfSome<Bgp>(b =>
                        And(b.GetCost() == new BigInteger(3), b.HasTag("B"))))
            }
        };

        return new Tags(topology, initialValues, annotations, new BigInteger(3),
            new Dictionary<Zen<Bgp>, Func<Zen<Bgp>, Zen<bool>>>());
    }

    private static Func<Zen<Bgp>, Zen<Option<Bgp>>> Transfer((string, string) edge)
    {
        var (src, snk) = edge;
        return x => If(x.HasTag(snk), Option.None<Bgp>(),
            Some(x.SetCost(x.GetCost() + new BigInteger(1)).AddTag(src)));
    }

    private static Zen<Bgp> Merge(Zen<Bgp> b1, Zen<Bgp> b2)
    {
        return If(b1.GetCost() < b2.GetCost(), b1, b2);
    }
}