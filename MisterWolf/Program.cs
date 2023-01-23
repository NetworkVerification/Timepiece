// See https://aka.ms/new-console-template for more information

using MisterWolf;
using Timepiece;
using ZenLib;

var topology = Topologies.Path(3);
var initialValues = topology.MapNodes(n => n == "A" ? Zen.True() : Zen.False());

// initially, the route can be anything
var beforeInvariants = topology.MapNodes(_ => Lang.True<bool>());
// eventually, it must be true
var afterInvariants = topology.MapNodes(_ => Lang.Identity<bool>());

var infer = new Infer<bool>(topology, topology.MapEdges(_ => Lang.Identity<bool>()), Zen.Or, initialValues,
  beforeInvariants, afterInvariants);

var times = infer.InferTimes();

if (times.Count > 0)
{
  Console.WriteLine("Success, inferred the following times:");
  foreach (var (node, time) in times)
  {
    Console.WriteLine($"{node}: {time}");
  }
}
else
{
  Console.WriteLine("Failed, could not infer times.");
}

var net = infer.ToNetwork<Unit>(times);
Profile.RunAnnotated(net);
