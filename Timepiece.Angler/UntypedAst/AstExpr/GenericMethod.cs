using System.Reflection;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public record GenericMethod(Type BaseType, string MethodName, params Type[] TypeArguments)
{
  private MethodInfo Method
  {
    get
    {
      var method = BaseType
        .GetMethods().Single(m => m.Name == MethodName && m.IsGenericMethodDefinition &&
                                  m.GetGenericArguments().Length == TypeArguments.Length);
      // throw new ArgumentException(
      // $"No method {MethodName} found for {BaseType} with {TypeArguments.Length} type arguments");
      return method.MakeGenericMethod(TypeArguments);
    }
  }

  public dynamic Call(params dynamic[] args) =>
    // BaseType.InvokeMember(MethodName, BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding, null, null, args)!;
    Method.Invoke(null, args)!;
}
