using Newtonsoft.Json;
using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class Or : AssociativeBinaryOpExpr
{
  private static readonly Func<dynamic, dynamic, Zen<bool>> Op = (e1, e2) => Zen.Or(e1, e2);

  private static Zen<bool> Method(dynamic e1, dynamic e2)
  {
    return Zen.Or(e1, e2);
  }

  public Or(Expr operand1, Expr operand2) : base(operand1, operand2, Method)
  {
  }

  [JsonConstructor]
  public Or(IEnumerable<Expr> exprs) : base(exprs, new ConstantExpr(false), Method)
  {
  }
}
