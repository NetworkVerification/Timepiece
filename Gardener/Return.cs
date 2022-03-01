using ZenLib;

namespace Gardener;

public class Return : Statement
{
  public Expr Expr { get; }

  public Return(Expr expr)
  {
    Expr = expr;
  }

  public override Func<Zen<dynamic>, Zen<dynamic>> ToZen()
  {
    throw new NotImplementedException();
  }

  public override Dictionary<string, dynamic> Evaluate(Dictionary<string, dynamic> state)
  {
    throw new NotImplementedException();
  }
}
