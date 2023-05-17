using Timepiece;
using ZenLib;

namespace MisterWolf;

public class Initialization<T>
{
  public Dictionary<string, Zen<T>> InitialValues { get; }

  public Initialization(Dictionary<string, Zen<T>> initialValues)
  {
    InitialValues = initialValues;
  }

  public Initialization(Topology topology, string destination, Zen<T> destinationRoute, T nullRoute) :
    this(topology.MapNodes(n => n == destination ? destinationRoute : nullRoute))
  {
  }
}
