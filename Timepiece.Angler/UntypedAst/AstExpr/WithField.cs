using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class WithField : BinaryOpExpr
{
  public WithField(Expr record, string fieldName, Expr fieldValue) : base(record,
    fieldValue, (r, v) => Zen.WithField(r, fieldName, v))
  {
    FieldName = fieldName;
  }

  public string FieldName { get; set; }
}
