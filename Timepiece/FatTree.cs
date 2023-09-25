using System;

namespace Timepiece;

public static class FatTree
{
  public enum FatTreeLayer
  {
    Edge,
    Aggregation,
    Core
  }

  public static bool IsCore(this FatTreeLayer l) => l == FatTreeLayer.Core;
  public static bool IsAggregation(this FatTreeLayer l) => l == FatTreeLayer.Aggregation;
  public static bool IsEdge(this FatTreeLayer l) => l == FatTreeLayer.Edge;

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
}
