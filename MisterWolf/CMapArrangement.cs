using System.Text;
using ZenLib;

namespace MisterWolf;

[ZenObject]
public class CMapArrangement<TV> where TV : notnull
{
  public CMapArrangement()
  {
    Invariant = Option.None<bool>();
    Neighbors = new CMap<TV, Option<bool>>();
  }

  public CMapArrangement(Option<bool> invariant, CMap<TV, Option<bool>> neighbors)
  {
    Invariant = invariant;
    Neighbors = neighbors;
  }

  public CMapArrangement(Option<bool> invariant, IEnumerable<(TV, Option<bool>)> neighbors) : this(invariant,
    neighbors.Aggregate(new CMap<TV, Option<bool>>(),
      (map, neighbor) => map.Set(neighbor.Item1, neighbor.Item2)))
  {
  }

  /// <summary>
  ///   Invariant at the node.
  /// </summary>
  public Option<bool> Invariant { get; set; }

  /// <summary>
  ///   Invariant at the node's neighbors.
  /// </summary>
  public CMap<TV, Option<bool>> Neighbors { get; set; }

  public Option<bool> this[TV index] => Neighbors.Get(index);

  private static string BoolToInvariantType(Option<bool> b)
  {
    return !b.HasValue ? "either" : b.Value ? "before" : "after";
  }

  public override string ToString()
  {
    var builder = new StringBuilder();
    foreach (var (m, b) in Neighbors.Values) builder.Append($"{BoolToInvariantType(b)}:{m}/");

    builder.Append($"{BoolToInvariantType(Invariant)}");
    return builder.ToString();
  }
}

public static class CMapArrangementExtensions
{
  public static Zen<Option<bool>> GetNeighbor<TNode>(this Zen<CMapArrangement<TNode>> arrangement, TNode neighbor)
    where TNode : notnull
  {
    return arrangement.GetNeighbors().Get(neighbor);
  }

  public static Zen<CSet<TNode>> SomeNeighbors<TNode>(this Zen<CMapArrangement<TNode>> arrangement,
    IEnumerable<TNode> neighbors) where TNode : notnull
  {
    return neighbors.Aggregate(CSet.Empty<TNode>(), (set, m) =>
      Zen.If(arrangement.GetNeighbor(m).IsSome(), set.Add(m), set));
  }
}
