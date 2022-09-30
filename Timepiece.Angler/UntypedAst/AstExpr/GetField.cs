using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class GetField : UnaryOpExpr
{
  private static GenericMethod GetMethod(Type recordTy, Type fieldTy) =>
    new(typeof(Zen), "GetField", recordTy, fieldTy);

  public GetField(Type recordTy, Type fieldTy, Expr record, string fieldName) : base(record,
    r => GetMethod(recordTy, fieldTy).Call(r, fieldName))
  {
  }
}
