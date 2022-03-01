using ZenLib;

namespace Gardener;

public class Seq : Statement
{
  public Statement First { get; set; }
  public Statement Second { get; set; }

  public Seq(Statement first, Statement second)
  {
    First = first;
    Second = second;
  }

  public override Func<Zen<dynamic>, Zen<dynamic>> ToZen()
  {
    return t => Second.ToZen()(First.ToZen()(t));
  }

  public override Dictionary<string, dynamic> Evaluate(Dictionary<string, dynamic> state)
  {
    return Second.Evaluate(First.Evaluate(state));
  }
}
