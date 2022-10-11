using Newtonsoft.Json;
using Timepiece.Angler.UntypedAst.AstExpr;

namespace Timepiece.Angler.UntypedAst.AstStmt;

public class IfThenElse : Statement
{
  public IfThenElse(Expr guard, IEnumerable<Statement> thenCase, IEnumerable<Statement> elseCase)
  {
    Guard = guard;
    ThenCase = thenCase;
    ElseCase = elseCase;
  }

  public Expr Guard { get; set; }
  public IEnumerable<Statement> ThenCase { get; set; }
  public IEnumerable<Statement> ElseCase { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    Guard.Rename(oldVar, newVar);
    foreach (var s in ThenCase)
    {
      s.Rename(oldVar, newVar);
    }

    foreach (var s in ElseCase)
    {
      s.Rename(oldVar, newVar);
    }
  }
}
