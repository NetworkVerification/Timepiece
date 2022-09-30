using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class WithField : BinaryOpExpr
{
  private static GenericMethod GetMethod(Type recordTy, Type fieldTy) =>
    new(typeof(Zen), "WithField", recordTy, fieldTy);

  public WithField(Type recordTy, Type fieldTy, Expr record, string fieldName, Expr fieldValue) : base(record,
    fieldValue, (r, v) => GetMethod(recordTy, fieldTy).Call(r, fieldName, v))
  {
  }
}
