using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Timepiece;

public static partial class FatTree
{
  [GeneratedRegex(@"(edge|aggregation|core)-(\d*)")]
  private static partial Regex FatTreeIntNodePattern();

  public enum FatTreeLayer
  {
    Edge,
    Aggregation,
    Core
  }

  public static bool IsCore(this FatTreeLayer l)
  {
    return l == FatTreeLayer.Core;
  }

  public static bool IsAggregation(this FatTreeLayer l)
  {
    return l == FatTreeLayer.Aggregation;
  }

  public static bool IsEdge(this FatTreeLayer l)
  {
    return l == FatTreeLayer.Edge;
  }

  // note: this needs to have a different name from ToString as we can't override that directly
  // so we give it this different name instead
  public static string ToLowerCaseString(this FatTreeLayer l)
  {
    return l switch
    {
      FatTreeLayer.Edge => "edge",
      FatTreeLayer.Aggregation => "aggregation",
      FatTreeLayer.Core => "core",
      _ => throw new ArgumentOutOfRangeException(nameof(l), l, $"{l} has no ToLowerCaseString implementation.")
    };
  }

  public static int IntNodeIndex(string s)
  {
    var match = FatTreeIntNodePattern().Match(s);
    if (!match.Success) throw new ArgumentException($"Invalid node name {s} has no parseable index.");
    return int.Parse(FatTreeIntNodePattern().Match(s).Groups[2].Value);
  }

  /// <summary>
  ///   Return a node representing the given node in the fat tree.
  /// </summary>
  /// <param name="l">The node's layer.</param>
  /// <param name="i">The node's index.</param>
  /// <returns>A string identifying this node.</returns>
  public static string Node<T>(this FatTreeLayer l, T i)
  {
    return $"{ToLowerCaseString(l)}-{i}";
  }

  public static FatTreeLayer Parse(this string s)
  {
    return s switch
    {
      "edge" => FatTreeLayer.Edge,
      "aggregation" => FatTreeLayer.Aggregation,
      "core" => FatTreeLayer.Core,
      _ => throw new ArgumentOutOfRangeException(nameof(s), s, $"{s} is not a valid fat tree layer.")
    };
  }

  public static bool IsCore(this string s)
  {
    return s.StartsWith(FatTreeLayer.Core.ToLowerCaseString());
  }

  public static bool IsAggregation(this string s)
  {
    return s.StartsWith(FatTreeLayer.Aggregation.ToLowerCaseString());
  }

  public static bool IsEdge(this string s)
  {
    return s.StartsWith(FatTreeLayer.Edge.ToLowerCaseString());
  }

  public static int DistanceFromDestinationEdge(this string s, int pod, string destination, int destinationPod)
  {
    return s == destination ? 0
      : s.IsAggregation() && pod == destinationPod ? 1
      : s.IsAggregation() && pod != destinationPod ? 3
      : s.IsEdge() && pod != destinationPod ? 4
      : 2;
  }

  /// <summary>
  ///   Infer the pod labels on a fat-tree digraph.
  ///   A k-fat-tree has (k^2)/2 core nodes, k^2 aggregation nodes and k^2 edge nodes.
  /// </summary>
  /// <param name="digraph">An (unlabelled) digraph.</param>
  /// <param name="skipNodes">Nodes not to label.</param>
  /// <returns>A node-labelled digraph.</returns>
  /// <exception cref="ArgumentException">If a node in the digraph does not have a name matching <see cref="FatTreeIntNodePattern"/></exception>
  /// <remarks>Skipped nodes receive the label -1.</remarks>
  public static NodeLabelledDigraph<string, int> LabelFatTree(Digraph<string> digraph, string[] skipNodes = null)
  {
    // the #nodes = 5/4 k^2 where k is the number of pods, so k = sqrt(#nodes * 4/5)
    // NB: we must subtract the skipped nodes from the #nodes!
    var skippedNodes = skipNodes?.Length ?? 0;
    var numberOfPods = (int) Math.Floor(Math.Sqrt((digraph.NNodes - skippedNodes) * 0.8));
    var coreNodes = digraph.Nodes.Count(s => s.IsCore());
    var labels = digraph.MapNodes(n =>
    {
      if (skipNodes is not null && skipNodes.Contains(n))
      {
        // if the node is skipped, we just give it a dummy label (we won't use it)
        return -1;
      }

      var match = FatTreeIntNodePattern().Match(n);
      if (!match.Success) throw new ArgumentException($"Given node {n} does not match the fat-tree node pattern!");
      var nodeNumber = int.Parse(match.Groups[2].Value);
      // if the node is a core node, then the groups are defined as the maximum pod number plus the core node's number
      if (IsCore(match.Groups[1].Value)) return numberOfPods + nodeNumber;
      // for aggregation and edge nodes, the pod number is going to be the quotient of
      // dividing the node number minus the number of core nodes by the number of pods:
      // pod # of node = (node # - #core) / #pods
      return (nodeNumber - coreNodes) / numberOfPods;
    });

    return new NodeLabelledDigraph<string, int>(digraph, labels);
  }
}
