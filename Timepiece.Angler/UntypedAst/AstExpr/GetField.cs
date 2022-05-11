using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class GetField : GenericExpr
{
  public Expr record;
  public string fieldName;

  public GetField(Type recordTy, Type fieldTy, Expr record, string fieldName)
  {
    TypeArguments = new[] {recordTy, fieldTy};
    BaseType = typeof(Zen);
    MethodName = "GetField";
    this.record = record;
    this.fieldName = fieldName;
  }

  public override void Rename(string oldVar, string newVar)
  {
    record.Rename(oldVar, newVar);
  }
}
