using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;

namespace Timepiece.Networks;

/// <summary>
/// A representation of a network with an asynchronous semantics,
/// where routes are sent according to a schedule defined via the
/// given activation and data flow functions.
/// </summary>
/// <typeparam name="NodeType"></typeparam>
/// <typeparam name="RouteType"></typeparam>
public class AsyncNetwork<RouteType, NodeType> : Network<RouteType, NodeType>
{
  public Dictionary<NodeType, RouteType> StartingRoutes { get; init; }
  public Func<Zen<BigInteger>, string, Zen<bool>> ActivationFunction { get; set; }
  public Dictionary<(NodeType, NodeType), Func<Zen<BigInteger>, Zen<BigInteger>>> DataFlowFunctions { get; set; }

  public AsyncNetwork(Digraph<NodeType> digraph,
    Dictionary<(NodeType, NodeType), Func<Zen<RouteType>, Zen<RouteType>>> transferFunction,
    Func<Zen<RouteType>, Zen<RouteType>, Zen<RouteType>> mergeFunction,
    Dictionary<NodeType, Zen<RouteType>> initialValues,
    ISymbolic[] symbolics, Dictionary<NodeType, RouteType> startingRoutes,
    Func<Zen<BigInteger>, string, Zen<bool>> activationFunction,
    Dictionary<(NodeType, NodeType), Func<Zen<BigInteger>, Zen<BigInteger>>> dataFlowFunctions) : base(digraph,
    transferFunction, mergeFunction, initialValues, symbolics)
  {
    StartingRoutes = startingRoutes;
    ActivationFunction = activationFunction;
    DataFlowFunctions = dataFlowFunctions;
  }
}
