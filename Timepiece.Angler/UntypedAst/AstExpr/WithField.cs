using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class WithField : GenericExpr
{
  public Expr record;
  public string fieldName;
  public Expr fieldValue;

  public WithField(Type recordTy, Type fieldTy, Expr record, string fieldName, Expr fieldValue)
  {
    BaseType = typeof(Zen);
    MethodName = "WithField";
    TypeArguments = new[] {recordTy, fieldTy};
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
