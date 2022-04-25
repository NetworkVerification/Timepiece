namespace Timekeeper.Json.UntypedAst;

public class Seq : Statement
{
  public Seq(Statement first, Statement second)
  {
    First = first;
    Second = second;
  }

  public Statement First { get; set; }
  public Statement Second { get; set; }

  public override void Rename(string oldVar, string newVar)
  {
    First.Rename(oldVar, newVar);
    Second.Rename(oldVar, newVar);
  }
}
