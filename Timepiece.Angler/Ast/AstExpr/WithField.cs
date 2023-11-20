using ZenLib;

namespace Timepiece.Angler.Ast.AstExpr;

public record WithField : BinaryOpExpr
{
  public WithField(Expr record, string fieldName, Expr fieldValue) : base(record,
    fieldValue, (r, v) => Zen.WithField(r, fieldName, v))
  {
    FieldName = fieldName;
  }

  public string FieldName { get; set; }
}
