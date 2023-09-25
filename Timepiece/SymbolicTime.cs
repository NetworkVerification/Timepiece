using System.Linq;
using System.Numerics;
using ZenLib;

namespace Timepiece;

/// <summary>
/// A symbolic value which represents a particular symbolically-chosen time.
/// </summary>
public class SymbolicTime : SymbolicValue<BigInteger>
{
  public SymbolicTime(string name) : base(name, t => t >= BigInteger.Zero)
  {
  }

  public SymbolicTime(string name, params SymbolicTime[] predecessors) : base(name,
    t => Zen.And(t >= BigInteger.Zero, Zen.And(predecessors.Select(pt => t > pt.Value))))
  {
  }
}
