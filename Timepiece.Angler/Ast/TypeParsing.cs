using System.Numerics;
using Timepiece.Angler.Ast.AstExpr;
using Timepiece.Angler.Ast.AstStmt;
using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Ast;

public static class TypeParsing
{
  /// <summary>
  ///   Delimiters between type names.
  /// </summary>
  private static readonly char[] Delimiters = {'(', ')', ',', ';'};

  /// <summary>
  ///   Convert a type string to a TypeAlias.
  /// </summary>
  /// <param name="typeName"></param>
  /// <returns></returns>
  public static TypeAlias ParseType(string typeName)
  {
    var types = typeName.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
    return ParseTypeAlias(types, 0).Item1;
  }

  /// <summary>
  /// Parse an array of strings into a TypeAlias, starting from the given index.
  /// </summary>
  /// <param name="alias">an array of strings representing the type name</param>
  /// <param name="index">the first string to look at in the array</param>
  /// <returns>a TypeAlias and the number of arguments consumed from the array</returns>
  /// <exception cref="ArgumentException">if a string in the array does not match a valid type alias</exception>
  private static (TypeAlias, int) ParseTypeAlias(string[] alias, int index)
  {
    if (!TryParse(alias[index], out var t) || !t.HasValue)
      throw new ArgumentException($"{alias} not a valid type alias.", nameof(alias));
    var step = 1;
    // if the type is non-generic, return it
    if (!t.Value.Type.IsGenericTypeDefinition) return (t.Value, step);
    // recursively search for each argument
    var args = t.Value.Args;
    var argsUsed = 0;
    while (argsUsed < args.Length)
    {
      if (args[argsUsed] is not null || argsUsed + 1 >= alias.Length) continue;
      // FIXME: this appears to both jump ahead in the array, AND change the starting point by returning argsUsed
      (args[argsUsed], step) = ParseTypeAlias(alias[(index + step)..], argsUsed);
      index += step;
      argsUsed++;
    }

    return (t.Value, argsUsed);
  }

  /// <summary>
  ///   Return a TypeAlias for the given string representing an AST expression, statement or type.
  /// </summary>
  /// <param name="s"></param>
  /// <param name="alias">The given type name string to bind.</param>
  /// <returns>The relevant TypeAlias.</returns>
  /// <exception cref="ArgumentOutOfRangeException">If the given string does not match any known term.</exception>
  public static bool TryParse(string s, out TypeAlias? alias)
  {
    alias = s switch
    {
      // statements
      "Assign" => typeof(Assign),
      "If" => typeof(IfThenElse),
      "SetDefaultPolicy" => typeof(SetDefaultPolicy),
      // expressions
      "Var" => new TypeAlias(typeof(Var), (TypeAlias?) null),
      "Call" => typeof(Call),
      // boolean expressions
      "Bool" => typeof(BoolExpr),
      "CallExprContext" => typeof(CallExprContext),
      "And" => typeof(And),
      "Or" => typeof(Or),
      "Not" => typeof(Not),
      "Havoc" => typeof(Havoc),
      "FirstMatchChain" => typeof(FirstMatchChain),
      "ConjunctionChain" => typeof(ConjunctionChain),
      // numeric expressions
      "Int32" => typeof(IntExpr),
      "BigInt" => typeof(BigIntExpr),
      "UInt2" => typeof(UInt2Expr),
      "UInt32" => typeof(UIntExpr),
      "Plus32" => typeof(Plus),
      "Sub32" => typeof(Sub),
      "LessThan" => typeof(BinaryOpExpr),
      "LessThanEqual" => typeof(BinaryOpExpr),
      "Equals32" => typeof(Equals),
      "NotEqual2" => typeof(NotEqual),
      // pair expressions
      "Pair" => new TypeAlias(typeof(PairExpr), null, null),
      "First" => new TypeAlias(typeof(First), null, null),
      "Second" => new TypeAlias(typeof(Second), null, null),
      // option expressions
      "Some" => new TypeAlias(typeof(Some), (TypeAlias?) null),
      "None" => new TypeAlias(typeof(None), (TypeAlias?) null),
      // record expressions
      "CreateRecord" => typeof(CreateRecord),
      "GetField" => typeof(GetField),
      "WithField" => typeof(WithField),
      // set expressions
      "String" => typeof(StringExpr),
      "Regex" => typeof(RegexExpr),
      "SetContains" => typeof(SetContains),
      "Subset" => typeof(Subset),
      "SetAdd" => typeof(SetAdd),
      "LiteralSet" => typeof(LiteralSet),
      "SetUnion" => typeof(SetUnion),
      "SetRemove" => typeof(SetRemove),
      "SetDifference" => typeof(SetDifference),
      // prefix expressions
      "IpPrefix" => typeof(PrefixExpr),
      "PrefixContains" => typeof(PrefixContains),
      "PrefixSet" => typeof(LiteralSet),
      "PrefixMatches" => typeof(PrefixMatches),
      "PrefixMatchSet" => typeof(PrefixMatchSet),
      // types
      "TEnvironment" => typeof(RouteEnvironment),
      "TRoute" => typeof(BatfishBgpRoute),
      "TResult" => typeof(RouteResult),
      "TIpAddress" => typeof(uint),
      "TIpPrefix" => typeof(Ipv4Wildcard),
      "RouteFilterList" => typeof(RouteFilterList),
      "TPrefixSet" => typeof(CSet<Ipv4Wildcard>), // TODO: is this ever used?
      "TPair" => new TypeAlias(typeof(Pair<,>), null, null),
      "TOption" => new TypeAlias(typeof(Option<>), (TypeAlias?) null),
      "TBool" => typeof(bool),
      "TInt2" => typeof(Int<_2>),
      "TUInt2" => typeof(UInt<_2>),
      "TInt32" => typeof(int),
      "TUInt32" => typeof(uint),
      "TTime" or "TBigInt" => typeof(BigInteger),
      "TString" => typeof(string),
      "TSet" => typeof(CSet<string>),
      _ => (TypeAlias?) null // we need to cast so that null doesn't get converted to a TypeAlias
    };
    return alias.HasValue;
  }
}
