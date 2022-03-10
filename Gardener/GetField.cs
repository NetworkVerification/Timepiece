using ZenLib;

namespace Gardener;

public class GetField<T1, T2, TState> : Expr<T2, TState>
{
  public Expr<T1, TState> Expr { get; set; }
  public string FieldName { get; set; }

  public GetField(Expr<T1, TState> expr, string fieldName)
  {
    Expr = expr;
    FieldName = fieldName;
  }
  public override Func<Zen<TState>, Zen<T2>> Evaluate(State<TState> state)
  {
    return r => Expr.Evaluate(state)(r).GetField<T1, T2>(FieldName);
  }
}
