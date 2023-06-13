using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;

namespace Timepiece;

/// <summary>
///   Represents the topology of an NV network as a directed graph.
///   We represent nodes as strings, with an associated set of predecessors
///   whose edges point to those nodes.
///   Using predecessors makes it efficient to represent our network semantics.
/// </summary>
public class Topology<TV> where TV : notnull
{
  /// <summary>
  ///   Construct a Topology given a mapping from nodes to their predecessors.
  /// </summary>
  [JsonConstructor]
  public Topology(Dictionary<TV, List<TV>> neighbors)
  {
    Neighbors = neighbors;
    NEdges = Neighbors.Sum(p => p.Value.Count);
    Nodes = Neighbors.Keys.ToArray();
  }

  /// <summary>
  ///   The number of edges in the network.
  /// </summary>
  [JsonIgnore]
  public int NEdges { get; }

  /// <summary>
  ///   The edges for each node in the network.
  /// </summary>
  [JsonPropertyName("edges")]
  public Dictionary<TV, List<TV>> Neighbors { get; set; }

  /// <summary>
  ///   The nodes in the network and their names.
  /// </summary>
  [JsonIgnore]
  public TV[] Nodes { get; set; }

  public TV this[uint id] => Nodes[id];

  /// <summary>
  ///   Return the predecessors of a given node.
  /// </summary>
  public List<TV> this[TV node] => Neighbors[node];

  /// <summary>
  ///   Return true if the topology contains the given node.
  /// </summary>
  /// <param name="node">A node.</param>
  /// <returns>True if the node is present, false otherwise.</returns>
  public bool HasNode(TV node)
  {
    return Neighbors.ContainsKey(node);
  }

  /// <summary>
  ///   Return a dictionary mapping each node in the topology with a given function.
  /// </summary>
  /// <param name="nodeFunc">The function over every node.</param>
  /// <typeparam name="T">The return type of the function.</typeparam>
  /// <returns>A dictionary representing the result of the function for every node.</returns>
  public Dictionary<TV, T> MapNodes<T>(Func<TV, T> nodeFunc)
  {
    return Nodes.ToDictionary(node => node, nodeFunc);
  }

  public TAcc FoldNodes<TAcc>(TAcc initial, Func<TAcc, TV, TAcc> f)
  {
    return Nodes.Aggregate(initial, f);
  }

  /// <summary>
  ///   Return all the edges in the network.
  /// </summary>
  /// <returns></returns>
  private IEnumerable<(TV, TV)> AllEdges()
  {
    return Neighbors
      .SelectMany(nodeNeighbors => nodeNeighbors.Value, (node, nbr) => (nbr, node.Key));
  }

  /// <summary>
  ///   Return a dictionary mapping each edge in the topology to a value according to the given function.
  /// </summary>
  /// <param name="edgeFunc"></param>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
  public Dictionary<(TV, TV), T> MapEdges<T>(Func<(TV, TV), T> edgeFunc)
  {
    return AllEdges().ToDictionary(e => e, edgeFunc);
  }

  public TAcc FoldEdges<TAcc>(TAcc initial, Func<TAcc, (TV, TV), TAcc> f)
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
      foreach (var neighbor in neighbors) builder.Append($"{neighbor}; ");

      builder.AppendLine();
    }

    return builder.ToString();
  }

  /// <summary>
  ///   Perform a backwards breadth-first search of the topology, starting from the goal node.
  ///   Return the distance from each node to the goal.
  ///   Note that nodes that cannot reach the goal will not appear in the returned dictionary.
  /// </summary>
  /// <param name="goal">The goal node.</param>
  /// <returns>A dictionary from nodes to their distance (number of edges) to the goal node.</returns>
  public Dictionary<TV, BigInteger> BreadthFirstSearch(TV goal)
  {
    var q = new Queue<TV>();
    var visited = new Dictionary<TV, BigInteger>
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
///   Represents the topology of an NV network with node labels.
/// </summary>
public class LabelledTopology<TV, T> : Topology<TV>
{
  public LabelledTopology(Dictionary<TV, List<TV>> neighbors, Dictionary<TV, T> labels) : base(neighbors)
  {
    Labels = labels;
  }

  /// <summary>
  ///   Labels for the nodes of the topology.
  /// </summary>
  public Dictionary<TV, T> Labels { get; }

  /// <summary>
  ///   Return the given node's label.
  /// </summary>
  /// <param name="node">A node in the topology.</param>
  /// <returns>The label for that node.</returns>
  public T L(TV node)
  {
    return Labels[node];
  }

  /// <summary>
  ///   Convert the LabelledTopology to an unlabelled one.
  /// </summary>
  /// <returns>An equivalent Topology.</returns>
  public Topology<TV> ToUnlabelled()
  {
    return new Topology<TV>(Neighbors);
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
  ///   Create a path digraph topology.
  /// </summary>
  /// <param name="numNodes">Number of nodes in topology.</param>
  /// <param name="alphaNames">
  /// If true, use strings of letters to name nodes, starting from 'A'; otherwise use numbers, starting from '0'.
  /// </param>
  /// <returns></returns>
  public static Topology<string> Path(uint numNodes, bool alphaNames = true)
  {
    var neighbors = new Dictionary<string, List<string>>();
    for (var i = 0; i < numNodes; i++) neighbors.Add(alphaNames ? ToBase26(i + 1) : i.ToString(), new List<string>());

    var nodes = neighbors.Keys.ToArray();
    for (var i = 1; i < numNodes; i++)
    {
      // add a pair of edges in sequence
      neighbors[nodes[i - 1]].Add(nodes[i]);
      neighbors[nodes[i]].Add(nodes[i - 1]);
    }

    return new Topology<string>(neighbors);
  }

  /// <summary>
  ///   Create a complete digraph topology.
  /// </summary>
  /// <param name="numNodes">Number of nodes in topology.</param>
  /// <param name="alphaNames">
  /// If true, use strings of letters to name nodes, starting from 'A'; otherwise use numbers, starting from '0'.
  /// </param>
  /// <returns></returns>
  public static Topology<string> Complete(uint numNodes, bool alphaNames = true)
  {
    var neighbors = new Dictionary<string, List<string>>();
    for (var i = 0; i < numNodes; i++) neighbors.Add(alphaNames ? ToBase26(i + 1) : i.ToString(), new List<string>());

    var nodes = neighbors.Keys;
    foreach (var (node, adj) in neighbors)
      // add all other nodes except the current one
      adj.AddRange(nodes.Where(n => n != node));

    return new Topology<string>(neighbors);
  }

  /// <summary>
  ///   Create a fattree topology of numPods pods.
  /// </summary>
  /// <param name="numPods">Number of pods in the fattree.</param>
  /// <returns></returns>
  public static Topology<string> FatTree(uint numPods)
  {
    return LabelledFatTree(numPods).ToUnlabelled();
  }

  /// <summary>
  ///   Create a labelled fattree topology of numPods pods.
  ///   Nodes are named "core-i", "aggregation-i" and "edge-i", where i is a non-negative integer.
  ///   Stores the pod number associated with each node: core nodes are each in their own k+i pod.
  /// </summary>
  /// <param name="numPods">Number of pods in the fattree.</param>
  /// <returns>A fattree topology with pod labels.</returns>
  public static LabelledTopology<string, int> LabelledFatTree(uint numPods)
  {
    var podNumbers = new Dictionary<string, int>();
    var neighbors = new Dictionary<string, List<string>>();
    var coreNodes = (int) Math.Floor(Math.Pow(numPods / 2.0, 2));
    // initialize neighbors for each node
    for (var i = 0; i < coreNodes; i++)
    {
      var name = Timepiece.FatTree.FatTreeLayer.Core.Node(i);
      podNumbers.Add(name, (int) numPods + i);
      neighbors.Add(name, new List<string>());
    }

    // construct each pod's edges
    for (var p = 0; p < numPods; p++)
    {
      var aggregates = new List<string>();
      var edges = new List<string>();
      var firstAggregateNode = neighbors.Count;
      var firstEdgeNode = firstAggregateNode + numPods / 2;
      var lastEdgeNode = firstEdgeNode + numPods / 2;
      // get pod's aggregation nodes
      for (var j = firstAggregateNode; j < firstEdgeNode; j++)
      {
        var name = Timepiece.FatTree.FatTreeLayer.Aggregation.Node(j);
        podNumbers.Add(name, p);
        aggregates.Add(name);
        neighbors.Add(name, new List<string>());
      }

      // get pod's edge nodes
      for (var k = firstEdgeNode; k < lastEdgeNode; k++)
      {
        var name = Timepiece.FatTree.FatTreeLayer.Edge.Node(k);
        podNumbers.Add(name, p);
        edges.Add(name);
        neighbors.Add(name, new List<string>());
      }

      // add all cross-tier edges
      foreach (var aggregate in aggregates)
      foreach (var edge in edges)
      {
        neighbors[aggregate].Add(edge);
        neighbors[edge].Add(aggregate);
      }
    }

    // add core-to-pod edges
    for (var c = 0; c < coreNodes; c++)
    {
      var coreNode = Timepiece.FatTree.FatTreeLayer.Core.Node(c);
      for (var p = 0; p < numPods; p++)
      {
        // TODO: use a system such as this to track which aggregate nodes go with which core nodes
        var aggregateNode =
          Timepiece.FatTree.FatTreeLayer.Aggregation.Node(coreNodes + c / (numPods / 2) + p * numPods);
        neighbors[coreNode].Add(aggregateNode);
        neighbors[aggregateNode].Add(coreNode);
      }
    }

    return new LabelledTopology<string, int>(neighbors, podNumbers);
  }
}
