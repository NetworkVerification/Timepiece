#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;

namespace Timepiece;

/// <summary>
///   An unweighted directed graph over a generic node type NodeType.
///   We represent the graph using an adjacency list with an associated set of predecessors
///   whose edges point to those nodes.
///   Using predecessors makes it efficient to represent our network semantics.
/// </summary>
public record Digraph<NodeType> where NodeType : notnull
{
  public virtual bool Equals(Digraph<NodeType>? other)
  {
    if (ReferenceEquals(null, other)) return false;
    if (ReferenceEquals(this, other)) return true;
    return Neighbors.Equals(other.Neighbors);
  }

  public override int GetHashCode()
  {
    return Neighbors.GetHashCode();
  }

  /// <summary>
  ///   Construct a Topology given a mapping from nodes to their predecessors.
  /// </summary>
  [JsonConstructor]
  public Digraph(IDictionary<NodeType, ImmutableSortedSet<NodeType>> neighbors)
  {
    Neighbors = neighbors;
    Nodes = Neighbors.Keys.ToArray();
  }

  public Digraph(IDictionary<NodeType, List<NodeType>> neighbors) : this(neighbors.Select(kvp =>
    new KeyValuePair<NodeType, ImmutableSortedSet<NodeType>>(kvp.Key, kvp.Value.ToImmutableSortedSet())))
  {
  }

  public Digraph(IEnumerable<KeyValuePair<NodeType, ImmutableSortedSet<NodeType>>> neighbors) : this(
    neighbors.ToDictionary(
      p => p.Key,
      p => p.Value))
  {
  }

  public Digraph(IEnumerable<KeyValuePair<NodeType, ICollection<NodeType>>> neighbors) : this(neighbors.ToDictionary(
    p => p.Key,
    p => p.Value.ToImmutableSortedSet()))
  {
  }

  /// <summary>
  ///   The number of edges.
  /// </summary>
  [JsonIgnore]
  public int NEdges => Neighbors.Sum(p => p.Value.Count);

  /// <summary>
  ///   The number of nodes.
  /// </summary>
  [JsonIgnore]
  public int NNodes => Nodes.Count;

  /// <summary>
  ///   The edges for each node.
  /// </summary>
  [JsonPropertyName("edges")]
  public IDictionary<NodeType, ImmutableSortedSet<NodeType>> Neighbors { get; }

  /// <summary>
  ///   The nodes in the graph and their names.
  /// </summary>
  [JsonIgnore]
  public IList<NodeType> Nodes { get; set; }

  public NodeType this[int id] => Nodes[id];

  /// <summary>
  ///   Return the predecessors of a given node.
  /// </summary>
  public ImmutableSortedSet<NodeType> this[NodeType node] => Neighbors[node];

  /// <summary>
  ///   Return true if the digraph contains the given node.
  /// </summary>
  /// <param name="node">A node.</param>
  /// <returns>True if the node is present, false otherwise.</returns>
  public bool HasNode(NodeType node)
  {
    return Neighbors.ContainsKey(node);
  }

  /// <summary>
  ///   Return true if the digraph contains the given edge.
  /// </summary>
  /// <param name="edge">An edge.</param>
  /// <returns>True if the edge is present, false otherwise.</returns>
  public bool HasEdge((NodeType, NodeType) edge)
  {
    return Neighbors.TryGetValue(edge.Item2, out var neighbors) && neighbors.Contains(edge.Item1);
  }

  /// <summary>
  ///   Add a new edge from a neighbor to the node.
  ///   If either the neighbor or the node are not already in the digraph, add them.
  /// </summary>
  /// <param name="node"></param>
  /// <param name="neighbor"></param>
  public void AddEdge(NodeType node, NodeType neighbor)
  {
    if (!HasNode(node))
    {
      Nodes.Add(node);
      Neighbors.Add(node, ImmutableSortedSet<NodeType>.Empty);
    }

    if (!HasNode(neighbor))
    {
      Nodes.Add(neighbor);
      Neighbors.Add(neighbor, ImmutableSortedSet<NodeType>.Empty);
    }

    // if the edge already exists, do nothing
    if (Neighbors[node].Contains(neighbor)) return;

    Neighbors.Add(node, Neighbors[node].Add(neighbor));
  }

  /// <summary>
  ///   Take the union of two graphs, such that the result contains all the nodes and edges of both.
  /// </summary>
  /// <param name="other"></param>
  /// <returns></returns>
  public Digraph<NodeType> Union(Digraph<NodeType> other)
  {
    var extended = Neighbors;
    foreach (var (node, neighbors) in other.Neighbors)
    {
      // add a new node if not previously in extended
      extended.TryAdd(node, ImmutableSortedSet.Create<NodeType>());
      extended[node] = extended[node].Union(neighbors);
    }

    return new Digraph<NodeType>(extended);
  }

  /// <summary>
  /// Return the induced subgraph of the digraph.
  /// The induced subgraph contains all of the nodes in the subset <paramref name="subsetNodes"/>
  /// and only edges that go between nodes in the subset.
  /// </summary>
  /// <param name="subsetNodes"></param>
  /// <returns></returns>
  public Digraph<NodeType> InducedSubgraph(IEnumerable<NodeType> subsetNodes)
  {
    var nodesArray = subsetNodes.ToArray();
    var inducedNeighbors = new Dictionary<NodeType, ImmutableSortedSet<NodeType>>();
    foreach (var node in nodesArray)
    {
      // skip the neighbor if it was already added (for handling duplicates)
      if (!inducedNeighbors.TryAdd(node, ImmutableSortedSet<NodeType>.Empty)) continue;
      foreach (var neighbor in this[node].Where(nodesArray.Contains))
      {
        inducedNeighbors[node] = inducedNeighbors[node].Add(neighbor);
      }
    }

    return new Digraph<NodeType>(inducedNeighbors);
  }

  /// <summary>
  ///   Return a dictionary mapping each node in the digraph with a given function.
  /// </summary>
  /// <param name="nodeFunc">The function over every node.</param>
  /// <typeparam name="T">The return type of the function.</typeparam>
  /// <returns>A dictionary representing the result of the function for every node.</returns>
  public Dictionary<NodeType, T> MapNodes<T>(Func<NodeType, T> nodeFunc)
  {
    return Nodes.ToDictionary(node => node, nodeFunc);
  }

  public TAcc FoldNodes<TAcc>(TAcc initial, Func<TAcc, NodeType, TAcc> f)
  {
    return Nodes.Aggregate(initial, f);
  }

  /// <summary>
  ///   Return all the edges in the network.
  /// </summary>
  /// <param name="edgeSelector">a predicate to select particular edges in the network</param>
  /// <returns>an enumerable over the edges of the network</returns>
  public IEnumerable<(NodeType, NodeType)> Edges(Func<(NodeType, NodeType), bool>? edgeSelector = null)
  {
    var edges = Neighbors
      .SelectMany(nodeNeighbors => nodeNeighbors.Value,
        (node, nbr) => (nbr, node.Key));
    return edgeSelector is null ? edges : edges.Where(edgeSelector);
  }

  /// <summary>
  ///   Return a dictionary mapping each edge in the digraph to a value according to the given function.
  /// </summary>
  /// <param name="edgeFunc">a function from an edge to a value T</param>
  /// <typeparam name="T">the type of the function output</typeparam>
  /// <returns></returns>
  public Dictionary<(NodeType, NodeType), T> MapEdges<T>(Func<(NodeType, NodeType), T> edgeFunc)
  {
    return Edges().ToDictionary(e => e, edgeFunc);
  }

  public TAcc FoldEdges<TAcc>(TAcc initial, Func<TAcc, (NodeType, NodeType), TAcc> f)
  {
    return Edges().Aggregate(initial, f);
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
  public Dictionary<NodeType, BigInteger> BreadthFirstSearch(NodeType goal)
  {
    var q = new Queue<NodeType>();
    var visited = new Dictionary<NodeType, BigInteger>
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
///   Represents the digraph of a network with node labels.
/// </summary>
public record NodeLabelledDigraph<NodeType, LabelType> : Digraph<NodeType> where NodeType : notnull
{
  public NodeLabelledDigraph(IDictionary<NodeType, ImmutableSortedSet<NodeType>> neighbors,
    Dictionary<NodeType, LabelType> labels) :
    base(neighbors)
  {
    Labels = labels;
  }

  public NodeLabelledDigraph(Digraph<NodeType> digraph, Dictionary<NodeType, LabelType> labels) : this(
    digraph.Neighbors, labels)
  {
  }

  /// <summary>
  ///   Labels for the nodes of the digraph.
  /// </summary>
  public Dictionary<NodeType, LabelType> Labels { get; }

  /// <summary>
  ///   Return the given node's label.
  /// </summary>
  /// <param name="node">A node in the digraph.</param>
  /// <returns>The label for that node.</returns>
  public LabelType L(NodeType node)
  {
    return Labels[node];
  }

  /// <summary>
  ///   Convert the NodeLabelledDigraph to an unlabelled one.
  /// </summary>
  /// <returns>An equivalent Digraph.</returns>
  public Digraph<NodeType> ToUnlabelled()
  {
    return new Digraph<NodeType>(Neighbors);
  }
}

public record EdgeLabelledDigraph<NodeType, LabelType> : Digraph<NodeType> where NodeType : notnull
{
  public EdgeLabelledDigraph(IDictionary<NodeType, ImmutableSortedSet<NodeType>> neighbors,
    Dictionary<(NodeType, NodeType), LabelType> labels) :
    base(neighbors)
  {
    Labels = labels;
  }

  /// <summary>
  ///   Labels for the edges in the digraph.
  /// </summary>
  public Dictionary<(NodeType, NodeType), LabelType> Labels { get; set; }

  public void AddEdge(NodeType node, NodeType neighbor, LabelType label)
  {
    Labels[(neighbor, node)] = label;
    AddEdge(node, neighbor);
  }
}
