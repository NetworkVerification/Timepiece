using Timekeeper.Json.UntypedAst.AstExpr;

namespace Timekeeper.Json.UntypedAst;

public class IfThenElse : Statement
{
  public IfThenElse(Expr guard, List<Statement> thenCase, List<Statement> elseCase)
  {
    Guard = guard;
    ThenCase = thenCase;
    ElseCase = elseCase;
  }

  public Expr Guard { get; set; }
  public List<Statement> ThenCase { get; set; }
  public List<Statement> ElseCase { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    Guard.Rename(oldVar, newVar);
    ThenCase.ForEach(s => s.Rename(oldVar, newVar));
    ElseCase.ForEach(s => s.Rename(oldVar, newVar));
  }
}
