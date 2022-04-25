namespace Timekeeper.Json.UntypedAst;

public class IfThenElse : Statement
{
  public IfThenElse(Expr guard, Statement thenCase, Statement elseCase)
  {
    Guard = guard;
    ThenCase = thenCase;
    ElseCase = elseCase;
  }

  public Expr Guard { get; set; }
  public Statement ThenCase { get; set; }
  public Statement ElseCase { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    Guard.Rename(oldVar, newVar);
    ThenCase.Rename(oldVar, newVar);
    ElseCase.Rename(oldVar, newVar);
  }
}
