using System;
using ZenLib;

namespace Timepiece;

public static class FatTree
{
  public enum FatTreeLayer
  {
    Edge,
    Aggregation,
    Core
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

  public static Zen<bool> IsCore(this Zen<string> s)
  {
    return s.StartsWith(FatTreeLayer.Core.ToLowerCaseString());
  }

  public static Zen<bool> IsAggregation(this Zen<string> s)
  {
    return s.StartsWith(FatTreeLayer.Aggregation.ToLowerCaseString());
  }

  public static Zen<bool> IsEdge(this Zen<string> s)
  {
    return s.StartsWith(FatTreeLayer.Edge.ToLowerCaseString());
  }
}
