using System.Text;
using ZenLib;

namespace MisterWolf;

[ZenObject]
public class Arrangement
{
  public Arrangement()
  {
    Invariant = Option.None<bool>();
    Neighbors = new FSeq<Option<bool>>();
  }

  public Arrangement(Option<bool> invariant, FSeq<Option<bool>> neighbors)
  {
    Invariant = invariant;
    Neighbors = neighbors;
  }

  public Arrangement(Option<bool> invariant, IEnumerable<Option<bool>> neighbors) : this(invariant,
    new FSeq<Option<bool>>(neighbors))
  {
  }

  /// <summary>
  ///   Invariant at the node.
  /// </summary>
  public Option<bool> Invariant { get; set; }

  /// <summary>
  ///   Invariant at the node's neighbors.
  /// </summary>
  public FSeq<Option<bool>> Neighbors { get; set; }

  public Option<bool> this[int index] => Neighbors.Values[index].Value;

  private static string BoolToInvariantType(Option<bool> b)
  {
    return !b.HasValue ? "either" : b.Value ? "before" : "after";
  }

  public override string ToString()
  {
    var builder = new StringBuilder();
    foreach (var b in Neighbors.Values.Where(b => b.HasValue)) builder.Append($"{BoolToInvariantType(b.Value)}/");

    builder.Append($"{BoolToInvariantType(Invariant)}");
    return builder.ToString();
  }
}

public static class ArrangementExtensions
{
  public static Zen<Arrangement> Symbolic(int numNeighbors)
  {
    var invariant = Zen.Symbolic<Option<bool>>();
    var neighbors = FSeq.Empty<Option<bool>>();
    for (var i = 0; i < numNeighbors; i++) neighbors = neighbors.AddFront(Zen.Symbolic<Option<bool>>());

    return Create(invariant, neighbors);
  }

  public static Zen<Arrangement> Create(Zen<Option<bool>> invariant, Zen<FSeq<Option<bool>>> neighbors)
  {
    return Zen.Constant(new Arrangement()).WithInvariant(invariant).WithNeighbors(neighbors);
  }

  public static Zen<Option<bool>> GetNeighbor(this Zen<Arrangement> arrangement, int neighbor)
  {
    return arrangement.GetNeighbors().At(neighbor).Value();
  }
}
