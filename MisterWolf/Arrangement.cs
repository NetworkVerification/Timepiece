using System.Text;
using ZenLib;

namespace MisterWolf;

[ZenObject]
public class Arrangement<TV> where TV : notnull
{
  public Arrangement(TV node, Option<bool> invariant, CMap<TV, Option<bool>> neighbors)
  {
    Node = node;
    Invariant = invariant;
    Neighbors = neighbors;
  }

  public Arrangement(TV node, Option<bool> invariant, IEnumerable<(TV, bool)> neighbors) : this(node, invariant,
    neighbors.Aggregate(new CMap<TV, Option<bool>>(),
      (map, neighbor) => map.Set(neighbor.Item1, Option.Some(neighbor.Item2))))
  {
  }

  /// <summary>
  /// Invariant at the node.
  /// </summary>
  public TV Node { get; set; }

  public Option<bool> Invariant { get; set; }

  /// <summary>
  /// Invariant at the node's neighbors.
  /// </summary>
  public CMap<TV, Option<bool>> Neighbors { get; set; }

  public Option<bool> this[TV index] => Neighbors.Get(index);

  private static string BoolToInvariantType(Option<bool> b) => !b.HasValue ? "either" : b.Value ? "before" : "after";

  public override string ToString()
  {
    var builder = new StringBuilder();
    foreach (var (m, b) in Neighbors.Values)
    {
      builder.Append($"{BoolToInvariantType(b)}:{m}/");
    }

    builder.Append($"{BoolToInvariantType(Invariant)}:{Node}");
    return builder.ToString();
  }
}

public static class ArrangementExtensions
{
  public static Zen<Arrangement<TNode>> Symbolic<TNode>(TNode node) where TNode : notnull
  {
    return Zen.Create<Arrangement<TNode>>(("Node", node), ("Invariant", Zen.Symbolic<Option<bool>>()),
      ("Neighbors", Zen.Symbolic<CMap<TNode, Option<bool>>>()));
  }

  public static Zen<Option<bool>> GetNeighbor<TNode>(this Zen<Arrangement<TNode>> arrangement, TNode neighbor)
    where TNode : notnull =>
    arrangement.GetNeighbors().Get(neighbor);

  public static Zen<CSet<TNode>> SomeNeighbors<TNode>(this Zen<Arrangement<TNode>> arrangement,
    IEnumerable<TNode> neighbors) where TNode : notnull =>
    neighbors.Aggregate(CSet.Empty<TNode>(), (set, m) =>
      Zen.If(arrangement.GetNeighbor(m).IsSome(), set.Add(m), set));
}
