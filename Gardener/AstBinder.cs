using System.Text.RegularExpressions;
using Gardener.AstExpr;
using Gardener.AstStmt;
using Newtonsoft.Json.Serialization;
using ZenLib;

namespace Gardener;

public class AstBinder<T> : ISerializationBinder
{
  private enum AstKind
  {
    Statement,
    Expr,
    Type
  }

  private readonly record struct TypeAlias(Type Type, AstKind[] Args);

  private IDictionary<string, TypeAlias> StatementAliasToType { get; }
  private IDictionary<string, TypeAlias> ExprAliasToType { get; }
  private IDictionary<string, Type> TyAliasToType { get; }

  public AstBinder()
  {
    StatementAliasToType = new Dictionary<string, TypeAlias>
    {
      {"Return", new TypeAlias(typeof(Return<>), new[] {AstKind.Statement})},
      {"Assign", new TypeAlias(typeof(Assign<>), new[] {AstKind.Expr})},
      {"If", new TypeAlias(typeof(IfThenElse<,>), new[] {AstKind.Statement, AstKind.Type})},
      {"Skip", new TypeAlias(typeof(Skip<>), new AstKind[] { })},
      {"Seq", new TypeAlias(typeof(Seq<,>), new[] {AstKind.Statement, AstKind.Type})}
    };
    ExprAliasToType = new Dictionary<string, TypeAlias>
    {
      {"Var", new TypeAlias(typeof(Var<>), new[] {AstKind.Type})},
      {"True", new TypeAlias(typeof(ConstantExpr<bool, T>), new AstKind[] {})},
      {"False", new TypeAlias(typeof(ConstantExpr<bool, T>), new AstKind[] {})},
      {"And", new TypeAlias(typeof(And<T>), new AstKind[] {})},
      {"Or", new TypeAlias(typeof(Or<T>), new AstKind[] {})},
      {"Not", new TypeAlias(typeof(Not<T>), new AstKind[] {})},
      {"Havoc", new TypeAlias(typeof(Havoc<T>), new AstKind[] {})},
      {"Int32", new TypeAlias(typeof(ConstantExpr<int, T>), new AstKind[] {})},
      {"Plus32", new TypeAlias(typeof(Plus<int, T>), new AstKind[] {})},
      {"Pair", new TypeAlias(typeof(PairExpr<,,>), new [] {AstKind.Expr, AstKind.Expr})},
      {"GetField", new TypeAlias(typeof(GetField<,,>), new [] {AstKind.Expr, AstKind.Expr, AstKind.Type})},
      {"WithField", new TypeAlias(typeof(WithField<,,>), new [] {AstKind.Expr, AstKind.Expr, AstKind.Expr})},
    };
    TyAliasToType = new Dictionary<string, Type>
    {
      {"Route", typeof(T)},
      {"Bool", typeof(bool)},
      {"Int32", typeof(int)},
      {"String", typeof(string)},
      {"Set", typeof(FBag<string>)},
      {"Unit", typeof(Unit)},
    };
  }

  private static IList<string> ParseTypeArgs(string typeName)
  {
    var types = new List<string>();
    const string pattern = @"(?<name>\w+)(?:\((?<args>.*)\))?";
    var regex = new Regex(pattern);
    foreach (Match match in regex.Matches(typeName))
    {
      var name = match.Groups["name"].Value;
      var args = match.Groups["args"];
      types.Add(name);
      // recursively call ParseTypeArgs on the subfields
      // construct a list for each type between the parentheses
    }

    return types;
  }


  public Type BindToType(string? assemblyName, string typeName)
  {
    // TODO: split up typeName and parse in sections
    var types = ParseTypeArgs(typeName);
    if (StatementAliasToType.ContainsKey(types[0]))
    {
      var t = StatementAliasToType[types[0]];
      if (t.Type.IsGenericTypeDefinition)
      {
        var args = new Type[t.Args.Length];
        // TODO fill in args
        foreach (var arg in t.Args)
        {
          switch (arg)
          {
            case AstKind.Statement:
              break;
            case AstKind.Expr:
              break;
            case AstKind.Type:
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
        }
        return t.Type.MakeGenericType(args);
      }
    }

    if (ExprAliasToType.ContainsKey(types[0]))
    {
      return ExprAliasToType[types[0]].Type;
    }

    if (TyAliasToType.ContainsKey(types[0]))
    {
      return TyAliasToType[types[0]];
    }

    throw new ArgumentException($"Unable to bind {typeName}");
  }

  public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
  {
    assemblyName = null;
    typeName = serializedType.Name;
  }
}
