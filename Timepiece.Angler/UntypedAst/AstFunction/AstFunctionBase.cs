using Newtonsoft.Json;

namespace Timepiece.Angler.UntypedAst.AstFunction;

public class AstFunctionBase<TBody> : IRenameable where TBody : IEnumerable<IRenameable>
{
  [JsonConstructor]
  public AstFunctionBase(string arg, TBody body)
  {
    Arg = arg;
    Body = body;
  }

  public string Arg { get; set; }
  public TBody Body { get; set; }

  public void Rename(string oldArg, string newArg)
  {
    if (Arg.Equals(oldArg)) Arg = newArg;
    foreach (var b in Body)
    {
      b.Rename(oldArg, newArg);
    }
  }
}
