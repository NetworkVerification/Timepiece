using ZenLib;
using ZenLib.ModelChecking;

namespace Timepiece;

public interface ISymbolic
{
  public string Name { get; }
  public Zen<bool> Encode();

  public bool HasConstraint();

  public object GetSolution(ZenSolution model);

  public string SolutionToString(ZenSolution model);
}
