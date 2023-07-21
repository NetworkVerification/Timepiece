using System.Text;
using ZenLib;

namespace MisterWolf;

[ZenObject]
public class Arrangement<TV> where TV : notnull
{
  public Arrangement(TV node, Option<bool> invariant, Dictionary<TV, Option<bool>> neighbors)
  {
    Node = node;
    Invariant = invariant;
    Neighbors = neighbors;
  }

  /// <summary>
  /// Invariant at the node.
  /// </summary>
  public TV Node { get; set; }

  public Option<bool> Invariant { get; set; }

  /// <summary>
  /// Invariant at the node's neighbors.
  /// </summary>
  public Dictionary<TV, Option<bool>> Neighbors { get; set; }

  private static string BoolToInvariantType(Option<bool> b) => !b.HasValue ? "either" : b.Value ? "before" : "after";

  public override string ToString()
  {
    var builder = new StringBuilder();
    foreach (var (m, b) in Neighbors)
    {
      builder.Append($"{BoolToInvariantType(b)}:{m}/");
    }

    builder.Append($"{BoolToInvariantType(Invariant)}:{Node}");
    return builder.ToString();
  }

  public IEnumerable<TResult> Select<TResult>(Func<TV, TResult> beforeCase, Func<TV, TResult> afterCase)
  {
    return Neighbors
      .Where(p => p.Value.HasValue)
      .Select(p => p.Value.Value ? beforeCase(p.Key) : afterCase(p.Key));
  }
}
