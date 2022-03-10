using ZenLib;

namespace Gardener;

public class WithField<T1, T2, TState> : Expr<T1, TState>
{
  public Expr<T1, TState> Expr { get; set; }
  public string FieldName { get; set; }

  public Expr<T2,TState> FieldValue { get; set; }

  public WithField(Expr<T1, TState> expr, string fieldName, Expr<T2, TState> fieldValue)
  {
    Expr = expr;
    FieldName = fieldName;
    FieldValue = fieldValue;
  }

  public override Func<Zen<TState>, Zen<T1>> Evaluate(State<TState> state)
  {
    return r => Expr.Evaluate(state)(r).WithField<T1, T2>(FieldName, FieldValue.Evaluate(state)(r));
  }
}
