namespace Timekeeper.Json.UntypedAst.AstExpr;

public class GetField : Expr
{
  public Type recordTy;
  public Type fieldTy;
  public Expr record;
  public string fieldName;

  public GetField(Type recordTy, Type fieldTy, Expr record, string fieldName)
  {
    this.recordTy = recordTy;
    this.fieldTy = fieldTy;
    this.record = record;
    this.fieldName = fieldName;
  }

  public override void Rename(string oldVar, string newVar)
  {
    record.Rename(oldVar, newVar);
  }
}
