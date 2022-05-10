namespace Timepiece.Angler.UntypedAst.AstExpr;

public class WithField : Expr
{
  public Type recordTy;
  public Type fieldTy;
  public Expr record;
  public string fieldName;
  public Expr fieldValue;

  public WithField(Type recordTy, Type fieldTy, Expr record, string fieldName, Expr fieldValue)
  {
    this.recordTy = recordTy;
    this.fieldTy = fieldTy;
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
