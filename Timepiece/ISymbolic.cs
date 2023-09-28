using ZenLib;
using ZenLib.ModelChecking;

namespace Timepiece;

public interface ISymbolic
{
  public Zen<bool> Encode();

  public bool HasConstraint();

  public string Name { get; }

  public object GetSolution(ZenSolution model);

  public string SolutionToString(ZenSolution model);
}
