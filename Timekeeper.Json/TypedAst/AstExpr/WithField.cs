using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

public class WithField<T1, T2, TState> : Expr<T1, TState>
{
  public WithField(Expr<T1, TState> record, string fieldName, Expr<T2, TState> fieldValue)
  {
    Record = record;
    FieldName = fieldName;
    FieldValue = fieldValue;
  }

  public Expr<T1, TState> Record { get; set; }
  public string FieldName { get; set; }

  public Expr<T2, TState> FieldValue { get; set; }

  public override Func<Zen<TState>, Zen<T1>> Evaluate(AstState<TState> astState)
  {
    var rf = Record.Evaluate(astState);
    var vf = FieldValue.Evaluate(astState);
    return r => rf(r).WithField(FieldName, vf(r));
  }

  public override void Rename(string oldVar, string newVar)
  {
    Record.Rename(oldVar, newVar);
    FieldValue.Rename(oldVar, newVar);
  }
}
