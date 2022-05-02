using ZenLib;

namespace Timekeeper.Json.TypedAst.AstExpr;

/// <summary>
///   An expression getting a named field from a record.
/// </summary>
/// <typeparam name="T1">The type of the record.</typeparam>
/// <typeparam name="T2">The type of the field.</typeparam>
/// <typeparam name="TState">The type of the evaluation astState.</typeparam>
public class GetField<T1, T2> : Expr<T2>
{
  public GetField(Expr<T1> record, string fieldName)
  {
    Record = record;
    FieldName = fieldName;
  }

  public Expr<T1> Record { get; set; }
  public string FieldName { get; set; }

  public override Func<Zen<TS>, Zen<T2>> Evaluate<TS>(AstState astState)
  {
    var f = Record.Evaluate<TS>(astState);
    return t => f(t).GetField<T1, T2>(FieldName);
  }

  public override void Rename(string oldVar, string newVar)
  {
    Record.Rename(oldVar, newVar);
  }
}
