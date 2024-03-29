using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Timepiece.Networks;
using Xunit;
using ZenLib;
using Array = System.Array;

namespace Timepiece.Tests;

public static class ShortestPathsTests
{
  private static readonly SymbolicValue<BigInteger> DRoute = new("d", r => r >= BigInteger.Zero);

  private static readonly ShortestPath<string> Concrete = new(Topologies.Path(3), "A",
    Array.Empty<ISymbolic>());

  private static readonly ShortestPath<string> SymbolicRoute = new(Topologies.Complete(3),
    new Dictionary<string, Zen<Option<BigInteger>>>
    {
      {"A", Option.Create(DRoute.Value)},
      {"B", Option.None<BigInteger>()},
      {"C", Option.None<BigInteger>()}
    }, new ISymbolic[] {DRoute});

  public static TheoryData<string, string[], Zen<Option<BigInteger>>> ExpectedRoutes => new()
  {
    {"A", new string[] { }, Option.Create<BigInteger>(BigInteger.Zero)},
    {"A", new[] {"B"}, Option.Create<BigInteger>(BigInteger.Zero)},
    {"B", new string[] { }, Option.Null<BigInteger>()},
    {"B", new[] {"A"}, Option.Create<BigInteger>(BigInteger.One)},
    {"B", new[] {"C"}, Option.Null<BigInteger>()},
    {"B", new[] {"A", "C"}, Option.Create<BigInteger>(BigInteger.One)}
  };

  public static TheoryData<Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<bool>>>> ConcreteProperties => new()
  {
    // weaker reachability property
    Concrete.Digraph.MapNodes(_ => Lang.IsSome<BigInteger>()),
    // stronger path length property
    new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<bool>>>
    {
      {"A", Lang.IfSome<BigInteger>(r => r == BigInteger.Zero)},
      {"B", Lang.IfSome<BigInteger>(r => r == BigInteger.One)},
      {"C", Lang.IfSome<BigInteger>(r => r == new BigInteger(2))}
    }
  };

  private static SymbolicValue<BigInteger>[] SymbolicWitnessTimes()
  {
    var aTime = new SymbolicTime("tau-A");
    var bTime = new SymbolicTime("tau-B", aTime);
    var cTime = new SymbolicTime("tau-C", bTime);
    return new SymbolicValue<BigInteger>[] {aTime, bTime, cTime};
  }

  [Theory]
  [MemberData(nameof(ExpectedRoutes))]
  public static void UpdateNodeRouteComputesExpectedValue(string node, string[] neighbors,
    Zen<Option<BigInteger>> expected)
  {
    var routes = new Dictionary<string, Zen<Option<BigInteger>>>
    {
      {"A", Option.Create<BigInteger>(BigInteger.Zero)},
      {"B", Option.Null<BigInteger>()},
      {"C", Option.Null<BigInteger>()}
    };
    var actual = Concrete.UpdateNodeRoute(node, routes, neighbors);
    var query = Zen.Not(Zen.Eq(actual, expected)).Solve();
    Assert.False(query.IsSatisfiable());
  }

  private static Zen<bool> IsInDigraph<NodeType>(Digraph<NodeType> digraph, Zen<NodeType> node)
    where NodeType : notnull
  {
    return digraph.FoldNodes(Zen.False(), (b, n) => Zen.Or(b, n == node));
  }

  private static SymbolicValue<string> SymbolicDestination(Digraph<string> digraph)
  {
    return new SymbolicValue<string>("dest", n => IsInDigraph(digraph, n));
  }

  private static ShortestPath<string> SymbolicDestinationShortestPath(Digraph<string> digraph,
    SymbolicValue<string> dest)
  {
    return new ShortestPath<string>(digraph,
      digraph.MapNodes(n => Zen.If(dest.EqualsValue(n),
        Option.Create<BigInteger>(BigInteger.Zero), Option.Null<BigInteger>())), new ISymbolic[] {dest});
  }

  private static AnnotatedNetwork<Option<BigInteger>, string> AnnotatedConcrete(
    Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BigInteger>>, Zen<bool>>> stableProperties)
  {
    return new AnnotatedNetwork<Option<BigInteger>, string>(Concrete, annotations,
      stableProperties, Concrete.Digraph.MapNodes(_ => Lang.True<Option<BigInteger>>()), 4);
  }

  private static AnnotatedNetwork<Option<BigInteger>, string> AnnotatedSymbolicRoute(
    Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<string, Func<Zen<Option<BigInteger>>, Zen<bool>>> stableProperties)
  {
    return new AnnotatedNetwork<Option<BigInteger>, string>(SymbolicRoute, annotations, stableProperties,
      SymbolicRoute.Digraph.MapNodes(_ => Lang.True<Option<BigInteger>>()), 2);
  }

  [Theory]
  [MemberData(nameof(ConcreteProperties))]
  public static void SoundAnnotationsPassChecks(Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<bool>>> properties)
  {
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals<Option<BigInteger>>(Option.Some(new BigInteger(0)))},
      {
        "B",
        Lang.Until(new BigInteger(1), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r == new BigInteger(1)))
      },
      {
        "C",
        Lang.Until(new BigInteger(2), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r == new BigInteger(2)))
      }
    };
    var net = AnnotatedConcrete(annotations, properties);

    NetworkAsserts.Sound(net);
  }

  [Fact]
  public static void UnsoundAnnotationsFailChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals<Option<BigInteger>>(Option.Some(new BigInteger(0)))},
      {"B", Lang.Never(Lang.IsSome<BigInteger>())},
      {"C", Lang.Never(Lang.IsSome<BigInteger>())}
    };
    var net = AnnotatedConcrete(annotations, new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<bool>>>
    {
      {"A", Lang.IfSome<BigInteger>(r => r == BigInteger.Zero)},
      {"B", Lang.IfSome<BigInteger>(r => r == BigInteger.One)},
      {"C", Lang.IfSome<BigInteger>(r => r == new BigInteger(2))}
    });

    NetworkAsserts.Unsound(net);
  }

  [Fact]
  public static void SoundSymbolicAnnotationsPassChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Equals(Option.Create(DRoute.Value))},
      {"B", Lang.Until(new BigInteger(1), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r >= DRoute.Value))},
      {"C", Lang.Until(new BigInteger(1), Lang.IsNone<BigInteger>(), Lang.IfSome<BigInteger>(r => r >= DRoute.Value))}
    };
    var net = AnnotatedSymbolicRoute(annotations, SymbolicRoute.Digraph.MapNodes(_ => Lang.IsSome<BigInteger>()));

    NetworkAsserts.Sound(net);
  }

  [Fact]
  public static void UnsoundSymbolicAnnotationsFailChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Globally(Lang.IfSome<BigInteger>(r => r <= DRoute.Value))},
      {"B", Lang.Finally(new BigInteger(1), Lang.IfSome<BigInteger>(r => r <= DRoute.Value))},
      {"C", Lang.Finally(new BigInteger(1), Lang.IfSome<BigInteger>(r => r <= DRoute.Value))}
    };
    var net = AnnotatedSymbolicRoute(annotations, SymbolicRoute.Digraph.MapNodes(_ => Lang.IsSome<BigInteger>()));

    NetworkAsserts.Unsound(net);
  }

  [Fact]
  public static void SoundSymbolicDestAnnotationsPassChecks()
  {
    var topology = Topologies.Path(3);
    var dest = SymbolicDestination(topology);
    var net = SymbolicDestinationShortestPath(topology, dest);
    var convergeTime = new BigInteger(3);
    var annotations =
      new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
      {
        {
          "A",
          Lang.Until<Option<BigInteger>>(
            Zen.If(dest.EqualsValue("A"), new BigInteger(0),
              Zen.If<BigInteger>(dest.EqualsValue("B"), new BigInteger(1), new BigInteger(2))),
            Option.IsNone, Option.IsSome)
        },
        {
          "B",
          Lang.Until<Option<BigInteger>>(
            Zen.If<BigInteger>(dest.DoesNotEqualValue("B"), new BigInteger(1), new BigInteger(0)),
            Option.IsNone, Option.IsSome)
        },
        {
          "C",
          Lang.Until<Option<BigInteger>>(
            Zen.If(dest.EqualsValue("A"), new BigInteger(2),
              Zen.If<BigInteger>(dest.EqualsValue("B"), new BigInteger(1), new BigInteger(0))),
            Option.IsNone, Option.IsSome)
        }
      };
    var annotated = new AnnotatedNetwork<Option<BigInteger>, string>(net, annotations,
      topology.MapNodes(_ => Lang.Finally(convergeTime, Lang.IsSome<BigInteger>())),
      topology.MapNodes(_ => Lang.IsSome<BigInteger>()));

    NetworkAsserts.Sound(annotated);
  }

  [Fact]
  public static void UnsoundSymbolicDestAnnotationsFailChecks()
  {
    var topology = Topologies.Path(3);
    var net = SymbolicDestinationShortestPath(topology, SymbolicDestination(topology));
    var annotations =
      new Dictionary<string, Func<Zen<Option<BigInteger>>, Zen<BigInteger>, Zen<bool>>>
      {
        {"A", Lang.Finally<Option<BigInteger>>(new BigInteger(1), Option.IsSome)},
        {"B", Lang.Finally<Option<BigInteger>>(new BigInteger(1), Option.IsSome)},
        {"C", Lang.Finally<Option<BigInteger>>(new BigInteger(1), Option.IsSome)}
      };
    var annotated = new AnnotatedNetwork<Option<BigInteger>, string>(net, annotations,
      topology.MapNodes(_ => Lang.Finally(new BigInteger(3), Lang.IsSome<BigInteger>())),
      topology.MapNodes(_ => Lang.IsSome<BigInteger>()));

    NetworkAsserts.Unsound(annotated, SmtCheck.Inductive);
  }

  [Fact]
  public static void CorrectlyOrderedAnnotationsPassChecks()
  {
    var symbolics = SymbolicWitnessTimes();
    var net = new ShortestPath<string>(Topologies.Path(3), "A", symbolics.Cast<ISymbolic>().ToArray());
    var annotations = net.Digraph.MapNodes(n =>
      Lang.Finally<Option<BigInteger>>(symbolics.First(s => s.Name.Equals($"tau-{n}")).Value, Option.IsSome));
    var annotated = new AnnotatedNetwork<Option<BigInteger>, string>(net, annotations,
      net.Digraph.MapNodes(_ => Lang.Finally(symbolics.Last().Value, Lang.IsSome<BigInteger>())),
      net.Digraph.MapNodes(_ => Lang.IsSome<BigInteger>()));

    NetworkAsserts.Sound(annotated);
  }

  [Fact]
  public static void UnconstrainedSymbolicTimesFailChecks()
  {
    var symbolics = SymbolicWitnessTimes();
    var lastTime = symbolics.Last().Value;
    var net = new ShortestPath<string>(Topologies.Path(3), "A", symbolics.Cast<ISymbolic>().ToArray());

    // weaken the symbolic constraints
    foreach (var symbolic in symbolics) symbolic.Constraint = x => x >= BigInteger.Zero;

    var annotations = net.Digraph.MapNodes(n =>
      Lang.Finally<Option<BigInteger>>(symbolics.First(s => s.Name.Equals($"tau-{n}")).Value, Option.IsSome));
    var annotated = new AnnotatedNetwork<Option<BigInteger>, string>(net, annotations,
      net.Digraph.MapNodes(_ => Lang.Finally(lastTime, Lang.IsSome<BigInteger>())),
      net.Digraph.MapNodes(_ => Lang.IsSome<BigInteger>()));

    NetworkAsserts.Unsound(annotated, SmtCheck.Inductive);
  }
}
