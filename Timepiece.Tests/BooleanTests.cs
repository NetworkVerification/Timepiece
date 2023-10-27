using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Timepiece.Networks;
using Xunit;
using ZenLib;
using static ZenLib.Zen;
using Array = System.Array;

namespace Timepiece.Tests;

public static class BooleanTests
{
  private static AnnotatedNetwork<bool, NodeType> BooleanAnnotatedNetwork<NodeType>(Network<bool, NodeType> net,
    Dictionary<NodeType, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations,
    Zen<BigInteger> convergeTime) where NodeType : notnull
  {
    return new AnnotatedNetwork<bool, NodeType>(net, annotations,
      net.Digraph.MapNodes(_ => Lang.Finally(convergeTime, Lang.Identity<bool>())),
      net.Digraph.MapNodes(_ => Lang.Identity<bool>()));
  }

  private static AnnotatedNetwork<bool, string> K2Net(
    Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations,
    Zen<BigInteger> convergeTime)
  {
    var topology = Topologies.Path(2);

    var initialValues = topology.MapNodes(n => Eq<string>(n, "A"));
    var net = new BooleanNetwork<string>(topology, initialValues, Array.Empty<ISymbolic>());

    return BooleanAnnotatedNetwork(net, annotations, convergeTime);
  }

  private static AnnotatedNetwork<bool, string> TinyWideAreaNet(Digraph<string> topology,
    Dictionary<string, SymbolicValue<bool>> externalRoutes,
    Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    // external peers start with their external routes
    var initialValues =
      topology.MapNodes(n => externalRoutes.TryGetValue(n, out var route) ? route.Value : False());
    var net = new BooleanNetwork<string>(topology, initialValues, externalRoutes.Values.Cast<ISymbolic>().ToArray());

    // we don't care what route externals have, but internals must eventually have a route
    var monolithicProperties = topology.MapNodes(n =>
      externalRoutes.ContainsKey(n)
        ? Lang.True<bool>()
        // if there is an external route, then eventually we want all internal nodes to have a route
        : r => Implies(Or(externalRoutes.Values.Select(ext => ext.Value)), r)
    );
    // internal nodes must all eventually have a route by time 2
    var modularProperties = topology.MapNodes(n =>
      externalRoutes.ContainsKey(n)
        ? Lang.Globally(monolithicProperties[n])
        : Lang.Finally(new BigInteger(2), monolithicProperties[n]));
    return new AnnotatedNetwork<bool, string>(net, annotations, modularProperties, monolithicProperties);
  }

  [Fact]
  public static void SoundAnnotationsPassChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Globally(Lang.Identity<bool>())},
      {"B", Lang.Finally(new BigInteger(1), Lang.Identity<bool>())}
    };
    var net = K2Net(annotations, new BigInteger(2));

    NetworkAsserts.Sound(net);
  }

  [Theory]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  [InlineData(100)]
  [InlineData(10000)]
  public static void SoundAnnotationsPassChecksDelayedMaxDelayConcrete(int max)
  {
    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Finally(new BigInteger(1), Lang.Identity<bool>())},
      {"B", Lang.Finally(new BigInteger(1 + max), Lang.Identity<bool>())}
    };
    // need the converge time to be the largest witness time, i.e. B's
    var net = K2Net(annotations, new BigInteger(1 + max));
    net.MaxDelay = new BigInteger(max);

    NetworkAsserts.Sound(net, SmtCheck.ModularDelayed);
  }

  /// <summary>
  ///   Test that the annotations pass for any symbolically-chosen maximum delay for a 3-node path network.
  /// </summary>
  [Fact]
  public static void SoundAnnotationsPassChecksDelayedMaxDelaySymbolic()
  {
    var topology = Topologies.Path(3);
    var initialValues = topology.MapNodes(n => Eq<string>(n, "A"));
    var delay = new SymbolicValue<BigInteger>("delay", d => d >= BigInteger.One);
    var net = new BooleanNetwork<string>(topology, initialValues, new ISymbolic[] {delay});

    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Finally(new BigInteger(1), Lang.Identity<bool>())},
      {"B", Lang.Finally(new BigInteger(1) + delay.Value, Lang.Identity<bool>())},
      {"C", Lang.Finally(new BigInteger(1) + new BigInteger(2) * delay.Value, Lang.Identity<bool>())}
    };
    // need the converge time to be the largest witness time, i.e. C's
    var annotated = BooleanAnnotatedNetwork(net, annotations, new BigInteger(2) * delay.Value + BigInteger.One);
    // NOTE: if we wanted, we could generalize this further to have an arbitrary delay on each edge
    annotated.MaxDelay = delay.Value;

    NetworkAsserts.Sound(annotated, SmtCheck.ModularDelayed);
  }

  [Fact]
  public static void UnsoundAnnotationsFailChecks()
  {
    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>
    {
      {"A", Lang.Globally(Lang.Identity<bool>())},
      {"B", Lang.Globally(Lang.Identity<bool>())}
    };
    var net = K2Net(annotations, new BigInteger(2));

    NetworkAsserts.Unsound(net);
  }

  [Fact]
  public static void FatTreeMonoChecks()
  {
    var topology = Topologies.FatTree(4);
    var initialValues =
      topology.MapNodes(n => Constant(n == FatTree.FatTreeLayer.Edge.Node(19)));
    var net = new BooleanNetwork<string>(topology, initialValues, Array.Empty<ISymbolic>());
    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>();
    var annotatedNetwork = BooleanAnnotatedNetwork(net, annotations, new BigInteger(4));
    // change edge-7's annotation to a bad property
    annotatedNetwork.MonolithicProperties[FatTree.FatTreeLayer.Edge.Node(7)] = _ => False();
    NetworkAsserts.Unsound(annotatedNetwork, SmtCheck.Monolithic);
  }

  [Fact]
  public static void TinyWanSoundAnnotationsPass()
  {
    // A-B-C is a connected WAN, and D, E and F are their external peers
    var topology = new Digraph<string>(new Dictionary<string, List<string>>
    {
      {"A", new List<string> {"B", "C", "D"}},
      {"B", new List<string> {"A", "C", "E"}},
      {"C", new List<string> {"A", "B", "F"}},
      {"D", new List<string> {"A"}},
      {"E", new List<string> {"B"}},
      {"F", new List<string> {"C"}}
    });
    var externalRoutes = new Dictionary<string, SymbolicValue<bool>>
    {
      {"D", new SymbolicValue<bool>("D-route")},
      {"E", new SymbolicValue<bool>("E-route")},
      {"F", new SymbolicValue<bool>("F-route")}
    };
    var annotations = new Dictionary<string, Func<Zen<bool>, Zen<BigInteger>, Zen<bool>>>
    {
      {
        "A",
        Lang.Finally<bool>(If<BigInteger>(externalRoutes["D"].Value, new BigInteger(1), new BigInteger(2)),
          b => Implies(Or(externalRoutes.Values.Select(s => s.Value)), b))
      },
      {
        "B",
        Lang.Finally<bool>(If<BigInteger>(externalRoutes["E"].Value, new BigInteger(1), new BigInteger(2)),
          b => Implies(Or(externalRoutes.Values.Select(s => s.Value)), b))
      },
      {
        "C",
        Lang.Finally<bool>(If<BigInteger>(externalRoutes["F"].Value, new BigInteger(1), new BigInteger(2)),
          b => Implies(Or(externalRoutes.Values.Select(s => s.Value)), b))
      },
      // if an external peer's route is true, then it should definitely share that.
      // otherwise, it will eventually receive a true route, like the internal peers
      {
        "D", Lang.Finally<bool>(If<BigInteger>(externalRoutes["D"].Value, BigInteger.Zero, new BigInteger(3)),
          b => Implies(Or(externalRoutes.Values.Select(s => s.Value)), b))
      },
      {
        "E", Lang.Finally<bool>(If<BigInteger>(externalRoutes["E"].Value, BigInteger.Zero, new BigInteger(3)),
          b => Implies(Or(externalRoutes.Values.Select(s => s.Value)), b))
      },
      {
        "F", Lang.Finally<bool>(If<BigInteger>(externalRoutes["F"].Value, BigInteger.Zero, new BigInteger(3)),
          b => Implies(Or(externalRoutes.Values.Select(s => s.Value)), b))
      }
    };
    var net = TinyWideAreaNet(topology, externalRoutes, annotations);
    NetworkAsserts.Sound(net);
  }
}
