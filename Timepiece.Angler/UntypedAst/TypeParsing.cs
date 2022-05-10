using System.Numerics;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstStmt;
using ZenLib;
using Regex = System.Text.RegularExpressions.Regex;

namespace Timepiece.Angler.UntypedAst;

public static class TypeParsing
{
  private static IEnumerable<string> ParseTypeArgs(string typeName)
  {
    var regex = new Regex(@"(?<term>\w+)");
    return regex.Matches(typeName).Select(match => match.Groups["term"].Value);
  }

  private static TypeAlias ParseTypeAlias(string alias, IEnumerator<string> typeArgs)
  {
    if (!TryParse(alias, out var t) || !t.HasValue) throw new ArgumentException(null, nameof(alias));
    if (!t.Value.Type.IsGenericTypeDefinition) return t.Value;
    // recursively search for each argument
    t.Value.UpdateArgs(typeArgs, args => ParseTypeAlias(args.Current, args).MakeType());
    return t.Value;
  }

  public static TypeAlias ParseType(string typeName)
  {
    var types = ParseTypeArgs(typeName).GetEnumerator();
    types.MoveNext();
    return ParseTypeAlias(types.Current, types);
  }

  /// <summary>
  /// Return a TypeAlias for the given string representing an AST expression, statement or type.
  /// </summary>
  /// <param name="s"></param>
  /// <param name="alias">The given type name string to bind.</param>
  /// <returns>The relevant TypeAlias.</returns>
  /// <exception cref="ArgumentOutOfRangeException">If the given string does not match any known term.</exception>
  private static bool TryParse(string s, out TypeAlias? alias)
  {
    alias = s switch
    {
      // statements
      "Return" => typeof(Return),
      "Assign" => typeof(Assign),
      "If" => typeof(IfThenElse),
      // expressions
      "Var" => new TypeAlias(typeof(Var), (TypeAlias?) null),
      // boolean expressions
      "True" => new TypeAlias(typeof(ConstantExpr), typeof(bool)),
      "False" => new TypeAlias(typeof(ConstantExpr), typeof(bool)),
      "And" => new TypeAlias(typeof(BinaryOpExpr), typeof(bool)),
      "Or" => new TypeAlias(typeof(BinaryOpExpr), typeof(bool)),
      "Not" => new TypeAlias(typeof(Not)),
      "Havoc" => typeof(Havoc),
      // numeric expressions
      "Int32" => new TypeAlias(typeof(ConstantExpr)),
      "BigInt" => new TypeAlias(typeof(ConstantExpr)),
      "Uint32" => new TypeAlias(typeof(ConstantExpr)),
      "Plus" => new TypeAlias(typeof(BinaryOpExpr)),
      "LessThan" => new TypeAlias(typeof(BinaryOpExpr)),
      "LessThanEqual" => new TypeAlias(typeof(BinaryOpExpr)),
      "Equal" => new TypeAlias(typeof(BinaryOpExpr)),
      // pair expressions
      "Pair" => new TypeAlias(typeof(BinaryOpExpr)),
      "First" => new TypeAlias(typeof(UnaryOpExpr)),
      "Second" => new TypeAlias(typeof(UnaryOpExpr)),
      // option expressions
      "Some" => new TypeAlias(typeof(UnaryOpExpr)),
      "None" => new TypeAlias(typeof(None)),
      // record expressions
      "GetField" => new TypeAlias(typeof(GetField)),
      "WithField" => new TypeAlias(typeof(WithField)),
      // set expressions
      "String" => new TypeAlias(typeof(ConstantExpr)),
      "SetContains" => new TypeAlias(typeof(BinaryOpExpr)),
      "SetAdd" => new TypeAlias(typeof(BinaryOpExpr)),
      "EmptySet" => new TypeAlias(typeof(ConstantExpr)),
      "SetUnion" => new TypeAlias(typeof(BinaryOpExpr)),
      // types
      "TRoute" => typeof(BatfishBgpRoute),
      "TPair" => new TypeAlias(typeof(Pair<,>), null, null),
      "TOption" => new TypeAlias(typeof(Option<>), (TypeAlias?) null),
      "TBool" => typeof(bool),
      "TInt32" => typeof(int),
      "TUint32" => typeof(uint),
      "TTime" or "TBigInt" => typeof(BigInteger),
      "TString" => typeof(string),
      "TSet" => typeof(Set<string>),
      "TUnit" => typeof(Unit),
      _ => (TypeAlias?) null, // we need to cast so that null doesn't get converted to a TypeAlias
    };
    return alias.HasValue;
  }
}
