using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Timepiece;

/// <summary>
/// Useful predefined graph topologies.
/// </summary>
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
  public static Digraph<string> Path(uint numNodes, bool alphaNames = true)
  {
    var neighbors = new Dictionary<string, ImmutableSortedSet<string>>();
    for (var i = 0; i < numNodes; i++)
      neighbors.Add(alphaNames ? ToBase26(i + 1) : i.ToString(), ImmutableSortedSet.Create<string>());

    var nodes = neighbors.Keys.ToArray();
    for (var i = 1; i < numNodes; i++)
    {
      // add a pair of edges in sequence
      neighbors[nodes[i - 1]] = neighbors[nodes[i - 1]].Add(nodes[i]);
      neighbors[nodes[i]] = neighbors[nodes[i]].Add(nodes[i - 1]);
    }

    return new Digraph<string>(neighbors);
  }

  /// <summary>
  ///   Create a complete digraph topology.
  /// </summary>
  /// <param name="numNodes">Number of nodes in topology.</param>
  /// <param name="alphaNames">
  /// If true, use strings of letters to name nodes, starting from 'A'; otherwise use numbers, starting from '0'.
  /// </param>
  /// <returns></returns>
  public static Digraph<string> Complete(uint numNodes, bool alphaNames = true)
  {
    var neighbors = new Dictionary<string, ImmutableSortedSet<string>>();
    for (var i = 0; i < numNodes; i++)
      neighbors.Add(alphaNames ? ToBase26(i + 1) : i.ToString(), ImmutableSortedSet<string>.Empty);

    var nodes = neighbors.Keys;
    foreach (var (node, adj) in neighbors)
      // add all other nodes except the current one
      neighbors[node] = adj.Union(nodes.Where(n => n != node));

    return new Digraph<string>(neighbors);
  }

  /// <summary>
  ///   Create a fat-tree topology of numPods pods.
  /// </summary>
  /// <param name="numPods">Number of pods in the fat-tree.</param>
  /// <returns></returns>
  public static Digraph<string> FatTree(uint numPods)
  {
    return LabelledFatTree(numPods).ToUnlabelled();
  }

  /// <summary>
  ///   Create a labelled fat-tree topology of numPods pods.
  ///   Nodes are named "core-i", "aggregation-i" and "edge-i", where i is a non-negative integer.
  ///   Stores the pod number associated with each node: core nodes are each in their own k+i pod.
  /// </summary>
  /// <param name="numPods">Number of pods in the fat-tree.</param>
  /// <returns>A fat-tree topology with pod labels.</returns>
  public static NodeLabelledDigraph<string, int> LabelledFatTree(uint numPods)
  {
    var podNumbers = new Dictionary<string, int>();
    var neighbors = new Dictionary<string, ImmutableSortedSet<string>>();
    var coreNodes = (int) Math.Floor(Math.Pow(numPods / 2.0, 2));
    // initialize neighbors for each node
    for (var i = 0; i < coreNodes; i++)
    {
      var name = Timepiece.FatTree.FatTreeLayer.Core.Node(i);
      podNumbers.Add(name, (int) numPods + i);
      neighbors.Add(name, ImmutableSortedSet<string>.Empty);
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
        neighbors.Add(name, ImmutableSortedSet<string>.Empty);
      }

      // get pod's edge nodes
      for (var k = firstEdgeNode; k < lastEdgeNode; k++)
      {
        var name = Timepiece.FatTree.FatTreeLayer.Edge.Node(k);
        podNumbers.Add(name, p);
        edges.Add(name);
        neighbors.Add(name, ImmutableSortedSet<string>.Empty);
      }

      // add all cross-tier edges
      foreach (var aggregate in aggregates)
      foreach (var edge in edges)
      {
        neighbors[aggregate] = neighbors[aggregate].Add(edge);
        neighbors[edge] = neighbors[edge].Add(aggregate);
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
        neighbors[coreNode] = neighbors[coreNode].Add(aggregateNode);
        neighbors[aggregateNode] = neighbors[aggregateNode].Add(coreNode);
      }
    }

    return new NodeLabelledDigraph<string, int>(neighbors, podNumbers);
  }
}
