namespace Timekeeper.Json.UntypedAst;

public class AstState
{
  public AstState(Dictionary<string, object> variables)
  {
    Variables = variables;
  }

  public AstState()
  {
    Variables = new Dictionary<string, object>();
  }

  public Dictionary<string, object> Variables { get; set; }

  public object this[string var]
  {
    get => Variables[var];
    set => Variables[var] = value;
  }
}
