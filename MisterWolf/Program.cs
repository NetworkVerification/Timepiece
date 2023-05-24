// See https://aka.ms/new-console-template for more information

using MisterWolf;
using Timepiece;
using ZenLib;

ZenSettings.UseLargeStack = true;
ZenSettings.LargeStackSize = 30_000_000;

// var topology = new Topology(new Dictionary<string, List<string>>
// {
// {"A", new List<string> {"B"}},
// {"B", new List<string> {"A", "C"}},
// {"C", new List<string> {"B"}},
// });
// var initialValues = topology.MapNodes(n => n.Equals("A") ? Zen.True() : Zen.False());
const int numPods = 4;
var topology = Topologies.LabelledFatTree(numPods);
var destination = FatTree.FatTreeLayer.Edge.Node((uint) (Math.Pow(numPods, 2) * 1.25 - 1));

if (args.Length == 0)
{
  Console.WriteLine("Please specify a benchmark to run!");
  return;
}

foreach (var arg in args)
{
  dynamic infer;
  Console.WriteLine($"Running benchmark {arg}...");
  switch (arg)
  {
    case "splen":
      var upperBounds = topology.MapNodes(n =>
      {
        if (n == destination)
        {
          return 0U;
        }

        if (n.IsAggregation() && topology.L(destination) == topology.L(n))
        {
          return 1U;
        }

        if (n.IsAggregation() && topology.L(destination) != topology.L(n))
        {
          return 3U;
        }

        if (n.IsEdge() && topology.L(destination) != topology.L(n))
        {
          return 4U;
        }

        return 2U;
      });
      infer = Benchmark.SingleDestinationOptionUintPathLength(topology, destination, upperBounds);
      break;
    default:
      throw new ArgumentOutOfRangeException(arg);
  }

  // uncomment to turn on verbose reporting of checks
  infer.PrintBounds = true;
  infer.MaxTime = 4;
  try
  {
    var net = infer.ToNetwork(InferenceStrategy.SymbolicEnumeration);
    Profile.RunAnnotated(net);
  }
  catch (ArgumentException e)
  {
    Console.WriteLine(e);
  }
}
