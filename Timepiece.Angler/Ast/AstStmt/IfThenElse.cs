using Timepiece.Angler.Ast.AstExpr;

namespace Timepiece.Angler.Ast.AstStmt;

public record IfThenElse : Statement
{
  public IfThenElse(Expr guard, IEnumerable<Statement> thenCase, IEnumerable<Statement> elseCase,
    string? comment = null)
  {
    Guard = guard;
    ThenCase = thenCase;
    ElseCase = elseCase;
    Comment = comment;
  }

  /// <summary>
  /// An optional comment describing the if statement.
  /// </summary>
  public string? Comment { get; set; }

  public Expr Guard { get; set; }
  public IEnumerable<Statement> ThenCase { get; set; }
  public IEnumerable<Statement> ElseCase { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    Guard.Rename(oldVar, newVar);
    foreach (var s in ThenCase) s.Rename(oldVar, newVar);

    foreach (var s in ElseCase) s.Rename(oldVar, newVar);
  }
}
