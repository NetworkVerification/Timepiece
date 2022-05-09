using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;

namespace Timepiece;

/// <summary>
/// Represents the topology of an NV network as a directed graph.
/// We represent nodes as strings, with an associated set of predecessors
/// whose edges point to those nodes.
/// Using predecessors makes it efficient to represent our network semantics.
/// </summary>
public class Topology
{
  /// <summary>
  /// Construct a Topology given a mapping from nodes to their predecessors.
  /// </summary>
  [JsonConstructor]
  public Topology(Dictionary<string, List<string>> neighbors)
  {
    Neighbors = neighbors;
    NEdges = Neighbors.Sum(p => p.Value.Count);
    Nodes = Neighbors.Keys.ToArray();
  }

  /// <summary>
  /// The number of edges in the network.
  /// </summary>
  [JsonIgnore]
  public int NEdges { get; private set; }

  /// <summary>
  /// The edges for each node in the network.
  /// </summary>
  [JsonPropertyName("edges")]
  public Dictionary<string, List<string>> Neighbors { get; set; }

  /// <summary>
  /// The nodes in the network and their names.
  /// </summary>
  [JsonIgnore]
  public string[] Nodes { get; set; }

  public string this[uint id] => Nodes[id];

  /// <summary>
  /// Return the predecessors of a given node.
  /// </summary>
  public List<string> this[string node] => Neighbors[node];

  /// <summary>
  /// Return true if the topology contains the given node.
  /// </summary>
  /// <param name="node">A node.</param>
  /// <returns>True if the node is present, false otherwise.</returns>
  public bool HasNode(string node)
  {
    return Neighbors.ContainsKey(node);
  }

  /// <summary>
  ///     Return a dictionary mapping each node in the topology with a given function.
  /// </summary>
  /// <param name="nodeFunc">The function over every node.</param>
  /// <typeparam name="T">The return type of the function.</typeparam>
  /// <returns>A dictionary representing the result of the function for every node.</returns>
  public Dictionary<string, T> ForAllNodes<T>(Func<string, T> nodeFunc)
  {
    return new Dictionary<string, T>(
      Nodes.Select(node => new KeyValuePair<string, T>(node, nodeFunc(node))));
  }

  public TAcc FoldNodes<TAcc>(TAcc initial, Func<TAcc, string, TAcc> f)
  {
    return Nodes.Aggregate(initial, f);
  }

  /// <summary>
  /// Return all the edges in the network.
  /// </summary>
  /// <returns></returns>
  private IEnumerable<(string, string)> AllEdges()
  {
    return Neighbors
      .SelectMany(nodeNeighbors => nodeNeighbors.Value, (node, nbr) => (nbr, node.Key));
  }

  public Dictionary<(string, string), T> ForAllEdges<T>(Func<(string, string), T> edgeFunc)
  {
    var edges = AllEdges().Select(e => new KeyValuePair<(string, string), T>(e, edgeFunc(e)));
    return new Dictionary<(string, string), T>(edges);
  }

  public TAcc FoldEdges<TAcc>(TAcc initial, Func<TAcc, (string, string), TAcc> f)
  {
    return AllEdges().Aggregate(initial, f);
  }

  public override string ToString()
  {
    var builder = new StringBuilder();
    builder.Append($"{Nodes.Length} nodes and {NEdges} edges");
    builder.AppendLine();
    foreach (var (node, neighbors) in Neighbors)
    {
      builder.Append($"{node}: ");
      foreach (var neighbor in neighbors)
      {
        builder.Append($"{neighbor}; ");
      }

      builder.AppendLine();
    }

    return builder.ToString();
  }

  /// <summary>
  /// Perform a backwards breadth-first search of the topology, starting from the goal node.
  /// Return the distance from each node to the goal.
  /// Note that nodes that cannot reach the goal will not appear in the returned dictionary.
  /// </summary>
  /// <param name="goal">The goal node.</param>
  /// <returns>A dictionary from nodes to their distance (number of edges) to the goal node.</returns>
  public Dictionary<string, BigInteger> BreadthFirstSearch(string goal)
  {
    var q = new Queue<string>();
    var visited = new Dictionary<string, BigInteger>
    {
      {goal, 0}
    };
    q.Enqueue(goal);
    while (q.Count > 0)
    {
      var n = q.Dequeue();
      var d = visited[n];

      foreach (var m in Neighbors[n].Where(m => !visited.ContainsKey(m)))
      {
        visited.Add(m, d + 1);
        q.Enqueue(m);
      }
    }

    return visited;
  }
}

/// <summary>
/// Represents the topology of an NV network with node labels.
/// </summary>
public class LabelledTopology<T> : Topology
{
  /// <summary>
  /// Labels for the nodes of the topology.
  /// </summary>
  public Dictionary<string, T> Labels { get; }

  public LabelledTopology(Dictionary<string, List<string>> neighbors, Dictionary<string, T> labels) : base(neighbors)
  {
    Labels = labels;
  }

  /// <summary>
  /// Return the given node's label.
  /// </summary>
  /// <param name="node">A node in the topology.</param>
  /// <returns>The label for that node.</returns>
  public T L(string node)
  {
    return Labels[node];
  }

  /// <summary>
  /// Convert the LabelledTopology to an unlabelled one.
  /// </summary>
  /// <returns>An equivalent Topology.</returns>
  public Topology ToUnlabelled()
  {
    return new Topology(Neighbors);
  }
}

public static class Topologies
{
  // helper method to generate node names ala Excel columns
  // adapted from https://stackoverflow.com/a/5384627
  private static string ToBase26(long i)
  {
    // the recursion adds the prefix
    if (i == 0) return "";
    i--;
    // the modulo is used to get the next char, looping back from 'Z' to 'A'
    return ToBase26(i / 26) + (char) ('A' + i % 26);
  }

  /// <summary>
  ///     Create a path digraph topology.
  /// </summary>
  /// <param name="numNodes">Number of nodes in topology.</param>
  /// <returns></returns>
  public static Topology Path(uint numNodes)
  {
    var neighbors = new Dictionary<string, List<string>>();
    for (var i = 0; i < numNodes; i++) neighbors.Add(ToBase26(i + 1), new List<string>());

    var nodes = neighbors.Keys.ToArray();
    for (var i = 1; i < numNodes; i++)
    {
      // add a pair of edges in sequence
      neighbors[nodes[i - 1]].Add(nodes[i]);
      neighbors[nodes[i]].Add(nodes[i - 1]);
    }

    return new Topology(neighbors);
  }

  /// <summary>
  /// Create a complete digraph topology.
  /// </summary>
  /// <param name="numNodes">Number of nodes in topology.</param>
  /// <returns></returns>
  public static Topology Complete(uint numNodes)
  {
    var neighbors = new Dictionary<string, List<string>>();
    for (var i = 0; i < numNodes; i++) neighbors.Add(ToBase26(i + 1), new List<string>());

    var nodes = neighbors.Keys;
    foreach (var (node, adj) in neighbors)
      // add all other nodes except the current one
      adj.AddRange(nodes.Where(n => n != node));

    return new Topology(neighbors);
  }

  /// <summary>
  /// Create a fattree topology of numPods pods.
  /// </summary>
  /// <param name="numPods">Number of pods in the fattree.</param>
  /// <returns></returns>
  public static Topology FatTree(uint numPods)
  {
    return LabelledFatTree(numPods).ToUnlabelled();
  }

  /// <summary>
  /// Create a labelled fattree topology of numPods pods.
  /// Nodes are named "core-i", "aggregation-i" and "edge-i", where i is a non-negative integer.
  /// Stores the pod number associated with each node: core nodes are in the last pod.
  /// </summary>
  /// <param name="numPods">Number of pods in the fattree.</param>
  /// <returns></returns>
  public static LabelledTopology<int> LabelledFatTree(uint numPods)
  {
    var podNumbers = new Dictionary<string, int>();
    var neighbors = new Dictionary<string, List<string>>();
    var coreNodes = (int) Math.Floor(Math.Pow(numPods / 2.0, 2));
    for (var i = 0; i < coreNodes; i++)
    {
      var name = Timepiece.FatTree.FatTreeLayer.Core.Node(i);
      podNumbers.Add(name, (int) numPods);
      neighbors.Add(name, new List<string>());
    }

    for (var p = 0; p < numPods; p++)
    {
      var aggregates = new List<string>();
      var edges = new List<string>();
      var firstAggregateNode = neighbors.Count;
      var firstEdgeNode = firstAggregateNode + numPods / 2;
      var lastEdgeNode = firstEdgeNode + numPods / 2;
      for (var j = firstAggregateNode; j < firstEdgeNode; j++)
      {
        var name = Timepiece.FatTree.FatTreeLayer.Aggregation.Node(j);
        podNumbers.Add(name, p);
        aggregates.Add(name);
        neighbors.Add(name, new List<string>());
      }

      for (var k = firstEdgeNode; k < lastEdgeNode; k++)
      {
        var name = Timepiece.FatTree.FatTreeLayer.Edge.Node(k);
        podNumbers.Add(name, p);
        edges.Add(name);
        neighbors.Add(name, new List<string>());
      }

      foreach (var aggregate in aggregates)
      {
        foreach (var edge in edges)
        {
          neighbors[aggregate].Add(edge);
          neighbors[edge].Add(aggregate);
        }
      }
    }

    for (var c = 0; c < coreNodes; c++)
    {
      var coreNode = Timepiece.FatTree.FatTreeLayer.Core.Node(c);
      for (var p = 0; p < numPods; p++)
      {
        var aggregateNode =
          Timepiece.FatTree.FatTreeLayer.Aggregation.Node(coreNodes + c / (numPods / 2) + p * numPods);
        neighbors[coreNode].Add(aggregateNode);
        neighbors[aggregateNode].Add(coreNode);
      }
    }

    return new LabelledTopology<int>(neighbors, podNumbers);
  }
}
