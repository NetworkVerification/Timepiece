using ZenLib;

namespace Timepiece.Angler.TypedAst.AstExpr;

public class WithField<T1, T2> : Expr<T1>
{
  public WithField(Expr<T1> record, string fieldName, Expr<T2> fieldValue)
  {
    Record = record;
    FieldName = fieldName;
    FieldValue = fieldValue;
  }

  public Expr<T1> Record { get; set; }
  public string FieldName { get; set; }

  public Expr<T2> FieldValue { get; set; }

  public override Zen<T1> Evaluate(AstState astState)
  {
    var r = Record.Evaluate(astState);
    var v = FieldValue.Evaluate(astState);
    return r.WithField(FieldName, v);
  }

  public override void Rename(string oldVar, string newVar)
  {
    Record.Rename(oldVar, newVar);
    FieldValue.Rename(oldVar, newVar);
  }
}
