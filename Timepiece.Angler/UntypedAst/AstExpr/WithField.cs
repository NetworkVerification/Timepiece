namespace Timepiece.Angler.UntypedAst.AstExpr;

public class WithField : Expr
{
  public Expr record;
  public string fieldName;
  public Expr fieldValue;

  public WithField(Expr record, string fieldName, Expr fieldValue)
  {
    this.record = record;
    this.fieldName = fieldName;
    this.fieldValue = fieldValue;
  }

  public override void Rename(string oldVar, string newVar)
  {
    record.Rename(oldVar, newVar);
    fieldValue.Rename(oldVar, newVar);
  }
}
