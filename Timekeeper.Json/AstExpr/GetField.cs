using ZenLib;

namespace Gardener.AstExpr;

/// <summary>
/// An expression getting a named field from a record.
/// </summary>
/// <typeparam name="T1">The type of the record.</typeparam>
/// <typeparam name="T2">The type of the field.</typeparam>
/// <typeparam name="TState">The type of the evaluation astState.</typeparam>
public class GetField<T1, T2, TState> : Expr<T2, TState>
{
  public Expr<T1, TState> Record { get; set; }
  public string FieldName { get; set; }

  public GetField(Expr<T1, TState> record, string fieldName)
  {
    Record = record;
    FieldName = fieldName;
  }
  public override Func<Zen<TState>, Zen<T2>> Evaluate(AstState<TState> astState)
  {
    var f = Record.Evaluate(astState);
    return r => f(r).GetField<T1, T2>(FieldName);
  }

  public override void Rename(string oldVar, string newVar)
  {
    Record.Rename(oldVar, newVar);
  }
}
