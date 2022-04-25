using Newtonsoft.Json;

namespace Timekeeper.Json.AstFunction;

public class AstFunctionBase<TArg, TBody> : IRenameable where TBody: IRenameable
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
    if (Arg.Equals(oldArg))
    {
      Arg = newArg;
    }
    Body.Rename(oldArg, newArg);
  }
}
