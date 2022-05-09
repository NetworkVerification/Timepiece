using ZenLib;

namespace Timepiece.Angler.TypedAst.AstStmt;

/// <summary>
///   A sequence of two statements.
///   The first statement must have a type of Unit,
///   while the second may have any return type.
/// </summary>
/// <typeparam name="T">The second statement's return type.</typeparam>
public class Seq<T> : Statement<T>
{
  public Seq(Statement<Unit> s1, Statement<T> s2)
  {
    S1 = s1;
    S2 = s2;
  }

  public Statement<Unit> S1 { get; set; }

  public Statement<T> S2 { get; set; }

  public override AstState Evaluate<TS>(AstState astState)
  {
    return S2.Evaluate<TS>(S1.Evaluate<TS>(astState));
  }

  public override Statement<Unit> Bind(string var)
  {
    return new Seq<Unit>(S1.Bind(var), S2.Bind(var));
  }

  public override void Rename(string oldVar, string newVar)
  {
    S1.Rename(oldVar, newVar);
    S2.Rename(oldVar, newVar);
  }
}
