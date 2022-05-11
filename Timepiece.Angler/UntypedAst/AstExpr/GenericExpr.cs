using System.Reflection;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public abstract class GenericExpr : Expr
{
  public Type BaseType { get; init; }

  public string MethodName { get; init; }

  public Type[] TypeArguments { get; init; }

  private MethodInfo Method
  {
    get
    {
      var method = BaseType.GetMethod(MethodName) ??
                   throw new InvalidOperationException($"{MethodName} not a method of {BaseType}");
      return method.MakeGenericMethod(TypeArguments);
    }
  }

  public dynamic Op(params dynamic[] args) => Method.Invoke(null, args)!;
}
