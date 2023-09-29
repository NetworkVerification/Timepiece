using System.Numerics;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Angler;

public class NetworkQuery<RouteType, NodeType> where NodeType : notnull
{
  public NetworkQuery(Dictionary<NodeType, Zen<RouteType>> initialRoutes, ISymbolic[] symbolics,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> monolithicProperties,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    InitialRoutes = initialRoutes;
    Symbolics = symbolics;
    MonolithicProperties = monolithicProperties;
    ModularProperties = modularProperties;
    Annotations = annotations;
  }

  public Dictionary<NodeType, Zen<RouteType>> InitialRoutes { get; set; }

  public ISymbolic[] Symbolics { get; set; }

  public Dictionary<NodeType, Func<Zen<RouteType>, Zen<bool>>> MonolithicProperties { get; set; }

  public Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> ModularProperties { get; set; }

  public Dictionary<NodeType, Func<Zen<RouteType>, Zen<BigInteger>, Zen<bool>>> Annotations { get; set; }

  public AnnotatedNetwork<RouteType, NodeType> ToNetwork(Digraph<NodeType> graph,
    Dictionary<(NodeType, NodeType), Func<Zen<RouteType>, Zen<RouteType>>> transferFunctions,
    Func<Zen<RouteType>, Zen<RouteType>, Zen<RouteType>> mergeFunction)
  {
    return new AnnotatedNetwork<RouteType, NodeType>(graph, transferFunctions, mergeFunction,
      InitialRoutes, Annotations, ModularProperties, MonolithicProperties, Symbolics);
  }
}
