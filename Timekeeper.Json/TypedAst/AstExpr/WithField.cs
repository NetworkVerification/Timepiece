using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

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

  public override Func<Zen<TS>, Zen<T1>> Evaluate<TS>(AstState astState)
  {
    var rf = Record.Evaluate<TS>(astState);
    var vf = FieldValue.Evaluate<TS>(astState);
    return t => rf(t).WithField(FieldName, vf(t));
  }

  public override void Rename(string oldVar, string newVar)
  {
    Record.Rename(oldVar, newVar);
    FieldValue.Rename(oldVar, newVar);
  }
}
