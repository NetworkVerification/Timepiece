using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;

namespace Timepiece;

/// <summary>
///   An unweighted directed graph over a generic node type TV.
///   We represent the graph using an adjacency list with an associated set of predecessors
///   whose edges point to those nodes.
///   Using predecessors makes it efficient to represent our network semantics.
/// </summary>
public class Digraph<TV> where TV : notnull
{
  /// <summary>
  ///   Construct a Topology given a mapping from nodes to their predecessors.
  /// </summary>
  [JsonConstructor]
  public Digraph(IDictionary<TV, ImmutableSortedSet<TV>> neighbors)
  {
    Neighbors = neighbors;
    NEdges = Neighbors.Sum(p => p.Value.Count);
    Nodes = Neighbors.Keys.ToArray();
  }

  public Digraph(IDictionary<TV, List<TV>> neighbors) : this(neighbors.Select(kvp =>
    new KeyValuePair<TV, ImmutableSortedSet<TV>>(kvp.Key, kvp.Value.ToImmutableSortedSet())))
  {
  }

  public Digraph(IEnumerable<KeyValuePair<TV, ImmutableSortedSet<TV>>> neighbors) : this(neighbors.ToDictionary(
    p => p.Key,
    p => p.Value))
  {
  }

  public Digraph(IEnumerable<KeyValuePair<TV, ICollection<TV>>> neighbors) : this(neighbors.ToDictionary(p => p.Key,
    p => p.Value.ToImmutableSortedSet()))
  {
  }

  /// <summary>
  ///   The number of edges.
  /// </summary>
  [JsonIgnore]
  public int NEdges { get; set; }

  /// <summary>
  ///   The edges for each node.
  /// </summary>
  [JsonPropertyName("edges")]
  public IDictionary<TV, ImmutableSortedSet<TV>> Neighbors { get; set; }

  /// <summary>
  ///   The nodes in the graph and their names.
  /// </summary>
  [JsonIgnore]
  public IList<TV> Nodes { get; set; }

  public TV this[int id] => Nodes[id];

  /// <summary>
  ///   Return the predecessors of a given node.
  /// </summary>
  public ImmutableSortedSet<TV> this[TV node] => Neighbors[node];

  /// <summary>
  ///   Return true if the digraph contains the given node.
  /// </summary>
  /// <param name="node">A node.</param>
  /// <returns>True if the node is present, false otherwise.</returns>
  public bool HasNode(TV node)
  {
    return Neighbors.ContainsKey(node);
  }

  /// <summary>
  /// Add a new edge from a neighbor to the node.
  /// </summary>
  /// <param name="node"></param>
  /// <param name="neighbor"></param>
  public void AddEdge(TV node, TV neighbor)
  {
    if (!HasNode(neighbor))
    {
      Nodes.Add(neighbor);
      Neighbors.Add(neighbor, ImmutableSortedSet<TV>.Empty);
    }

    Neighbors.Add(node, Neighbors[node].Add(neighbor));
    NEdges++;
  }

  /// <summary>
  /// Take the union of two graphs, such that the result contains all the nodes and edges of both.
  /// </summary>
  /// <param name="other"></param>
  /// <returns></returns>
  public Digraph<TV> Union(Digraph<TV> other)
  {
    var extended = Neighbors;
    foreach (var (node, neighbors) in other.Neighbors)
    {
      if (extended.ContainsKey(node))
      {
        extended[node] = extended[node].Union(neighbors);
      }
      else
      {
        extended[node] = neighbors;
      }
    }

    return new Digraph<TV>(extended);
  }

  /// <summary>
  ///   Return a dictionary mapping each node in the digraph with a given function.
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
  /// <returns>an enumerable over the edges of the network</returns>
  private IEnumerable<(TV, TV)> AllEdges()
  {
    return Neighbors
      .SelectMany(nodeNeighbors => nodeNeighbors.Value, (node, nbr) => (nbr, node.Key));
  }

  /// <summary>
  ///   Return a dictionary mapping each edge in the digraph to a value according to the given function.
  /// </summary>
  /// <param name="edgeFunc">a function from an edge to a value T</param>
  /// <typeparam name="T">the type of the function output</typeparam>
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
    builder.Append($"{Nodes.Count} nodes and {NEdges} edges");
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
  ///   Perform a backwards breadth-first search of the digraph, starting from the goal node.
  ///   Return the distance from each node to the goal as a BigInteger.
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
///   Represents the digraph of an NV network with node labels.
/// </summary>
public class LabelledDigraph<TV, T> : Digraph<TV>
{
  public LabelledDigraph(IDictionary<TV, ImmutableSortedSet<TV>> neighbors, Dictionary<TV, T> labels) : base(neighbors)
  {
    Labels = labels;
  }

  /// <summary>
  ///   Labels for the nodes of the digraph.
  /// </summary>
  public Dictionary<TV, T> Labels { get; }

  /// <summary>
  ///   Return the given node's label.
  /// </summary>
  /// <param name="node">A node in the digraph.</param>
  /// <returns>The label for that node.</returns>
  public T L(TV node)
  {
    return Labels[node];
  }

  /// <summary>
  ///   Convert the LabelledDigraph to an unlabelled one.
  /// </summary>
  /// <returns>An equivalent Digraph.</returns>
  public Digraph<TV> ToUnlabelled()
  {
    return new Digraph<TV>(Neighbors);
  }
}
