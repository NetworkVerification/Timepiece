using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Networks;

public class NondeterministicAnnotatedNetwork<RouteType, NodeType> : AnnotatedNetwork<RouteType, NodeType>,
  IAnnotated<RouteType, NodeType>
{
  /// <summary>
  /// Check the inductive condition, treating the MergeFunction as a nondeterministic choice.
  /// </summary>
  /// <param name="node"></param>
  /// <param name="routes"></param>
  /// <param name="time"></param>
  /// <returns></returns>
  public new Option<State<RouteType, NodeType>> CheckInductive(NodeType node,
    IReadOnlyDictionary<NodeType, Zen<RouteType>> routes, Zen<BigInteger> time)
  {
    // Instead of the standard UpdateNodeRoute, we define the route to be equal to one of the possible choices.
    var newNodeRoute = Symbolic<RouteType>($"updated-{node}-route");
    var choices =
      Digraph[node].Select(nbr => TransferFunctions[(nbr, node)](routes[nbr])).Append(InitialValues[node])
        .Exists(choice => newNodeRoute == choice);

    var assume = new List<Zen<bool>> {time > new BigInteger(0)};
    assume.AddRange(Digraph[node].Select(neighbor =>
      Annotations[neighbor](routes[neighbor], time - new BigInteger(1))));

    var check = Implies(And(assume.ToArray()), And(choices, Annotations[node](newNodeRoute, time)));

    // negate and try to prove unsatisfiable.
    var query = And(GetSymbolicConstraints(), Not(check));
    if (PrintFormulas)
    {
      Console.WriteLine($"Inductive check at {node}: ");
      Console.WriteLine(query.Format());
    }

    var model = query.Solve();

    if (!model.IsSatisfiable()) return Option.None<State<RouteType, NodeType>>();
    var neighborRoutes = routes.Where(pair => Digraph[node].Contains(pair.Key));
    var state = new State<RouteType, NodeType>(model, node, newNodeRoute, neighborRoutes, time,
      Symbolics);
    return Option.Some(state);
  }

  public NondeterministicAnnotatedNetwork(Digraph<NodeType> digraph,
    Dictionary<(NodeType, NodeType), Func<Zen<RouteType>, Zen<RouteType>>> transferFunctions,
    Func<Zen<RouteType>, Zen<RouteType>, Zen<RouteType>> mergeFunction,
    Dictionary<NodeType, Zen<RouteType>> initialValues,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> monolithicProperties, ISymbolic[] symbolics) : base(digraph,
    transferFunctions, mergeFunction, initialValues, annotations, modularProperties, monolithicProperties, symbolics)
  {
  }

  public NondeterministicAnnotatedNetwork(Digraph<NodeType> digraph,
    Dictionary<(NodeType, NodeType), Func<Zen<RouteType>, Zen<RouteType>>> transferFunctions,
    Func<Zen<RouteType>, Zen<RouteType>, Zen<RouteType>> mergeFunction,
    Dictionary<NodeType, Zen<RouteType>> initialValues,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> safetyProperties, BigInteger convergeTime,
    ISymbolic[] symbolics) : base(digraph, transferFunctions, mergeFunction, initialValues, annotations,
    stableProperties, safetyProperties, convergeTime, symbolics)
  {
  }

  public NondeterministicAnnotatedNetwork(Network<RouteType, NodeType> net,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> monolithicProperties) : base(net, annotations,
    modularProperties, monolithicProperties)
  {
  }

  public NondeterministicAnnotatedNetwork(Network<RouteType, NodeType> net,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> annotations,
    IReadOnlyDictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> stableProperties,
    IReadOnlyDictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> safetyProperties, BigInteger convergeTime) : base(
    net, annotations, stableProperties, safetyProperties, convergeTime)
  {
  }
}
