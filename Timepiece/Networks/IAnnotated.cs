using System.Collections.Generic;
using System.Numerics;
using ZenLib;

namespace Timepiece.Networks;

public interface IAnnotated<RouteType, NodeType>
{
  public Option<State<RouteType, NodeType>> CheckInitial(NodeType node);
  public Option<State<RouteType, NodeType>> CheckInitial();

  public Option<State<RouteType, NodeType>> CheckInductive(NodeType node);

  public Option<State<RouteType, NodeType>> CheckInductive(NodeType node,
    IReadOnlyDictionary<NodeType, Zen<RouteType>> routes, Zen<BigInteger> time);

  public Option<State<RouteType, NodeType>> CheckInductive();

  public Option<State<RouteType, NodeType>> CheckSafety(NodeType node);
  public Option<State<RouteType, NodeType>> CheckSafety();


  public Option<State<RouteType, NodeType>> CheckAnnotations(NodeType node);
  public Option<State<RouteType, NodeType>> CheckAnnotations();
}
