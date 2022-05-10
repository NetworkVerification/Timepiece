using System.Numerics;
using Newtonsoft.Json.Serialization;
using Timepiece.Angler.TypedAst.AstExpr;
using Timepiece.Angler.TypedAst.AstFunction;
using Timepiece.Angler.TypedAst.AstStmt;
using ZenLib;
using Regex = System.Text.RegularExpressions.Regex;

namespace Timepiece.Angler.TypedAst;

public class AstSerializationBinder<TRoute, TState> : ISerializationBinder
{
  private static readonly Type State = typeof(TState);

  public Type BindToType(string? assemblyName, string typeName)
  {
    switch (typeName)
    {
      case "Finally":
        return typeof(Finally<>).MakeGenericType(State);
      case "Globally":
        return typeof(Globally<>).MakeGenericType(State);
      case "Until":
        return typeof(Until<>).MakeGenericType(State);
      default:
        var types = ParseTypeArgs(typeName).GetEnumerator();
        types.MoveNext();
        return BindToTypeAux(types.Current, types);
    }
  }

  public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
  {
    assemblyName = null;
    typeName = serializedType.Name;
  }

  /// <summary>
  /// Return a TypeAlias for the given string representing an AST expression, statement or type.
  /// </summary>
  /// <param name="alias">The given type name string to bind.</param>
  /// <returns>The relevant TypeAlias.</returns>
  /// <exception cref="ArgumentOutOfRangeException">If the given string does not match any known term.</exception>
  private static TypeAlias GetAliasType(string alias)
  {
    return alias switch
    {
      // statements
      "Return" => new TypeAlias(typeof(Return<>), (TypeAlias?) null),
      "Assign" => new TypeAlias(typeof(Assign<>), (TypeAlias?) null),
      "If" => new TypeAlias(typeof(IfThenElse<>), (TypeAlias?) null),
      "Skip" => typeof(Skip),
      "Seq" => new TypeAlias(typeof(AstStmt.Seq<>), (TypeAlias?) null),
      // expressions
      "Var" => new TypeAlias(typeof(Var<>), (TypeAlias?) null),
      // boolean expressions
      "Bool" => typeof(ConstantExpr<bool>),
      "And" => typeof(And),
      "Or" => typeof(Or),
      "Not" => typeof(Not),
      "Havoc" => typeof(Havoc),
      // numeric expressions
      "Int32" => typeof(ConstantExpr<int>),
      "BigInt" => typeof(ConstantExpr<BigInteger>),
      "Uint32" => typeof(ConstantExpr<uint>),
      "Plus" => new TypeAlias(typeof(Plus<>), (TypeAlias?) null),
      "LessThan" => new TypeAlias(typeof(LessThan<>), (TypeAlias?) null),
      "LessThanEqual" => new TypeAlias(typeof(LessThanEqual<>), (TypeAlias?) null),
      "Equal" => new TypeAlias(typeof(Equal<>), (TypeAlias?) null),
      // pair expressions
      "Pair" => new TypeAlias(typeof(PairExpr<,>), null, null),
      "First" => new TypeAlias(typeof(First<,>), null, null),
      "Second" => new TypeAlias(typeof(Second<,>), null, null),
      // option expressions
      "Some" => new TypeAlias(typeof(Some<>), (TypeAlias?) null),
      "None" => new TypeAlias(typeof(None<>), (TypeAlias?) null),
      // record expressions
      "GetField" => new TypeAlias(typeof(GetField<,>), null, null),
      "WithField" => new TypeAlias(typeof(WithField<,>), null, null),
      // set expressions
      "String" => typeof(ConstantExpr<string>),
      "SetContains" => typeof(SetContains),
      "SetAdd" => typeof(SetAdd),
      "EmptySet" => typeof(EmptySet),
      "SetUnion" => typeof(SetUnion),
      // types
      "TRoute" => typeof(TRoute),
      "TPair" => new TypeAlias(typeof(Pair<,>), null, null),
      "TOption" => new TypeAlias(typeof(Option<>), (TypeAlias?) null),
      "TBool" => typeof(bool),
      "TInt32" => typeof(int),
      "TUint32" => typeof(uint),
      "TTime" or "TBigInt" => typeof(BigInteger),
      "TString" => typeof(string),
      "TSet" => typeof(Set<string>),
      "TUnit" => typeof(Unit),
      _ => throw new ArgumentOutOfRangeException(nameof(alias))
    };
  }


  private static IEnumerable<string> ParseTypeArgs(string typeName)
  {
    var regex = new Regex(@"(?<term>\w+)");
    return regex.Matches(typeName).Select(match => match.Groups["term"].Value);
  }

  private static Type BindToTypeAux(string alias, IEnumerator<string> typeArgs)
  {
    var t = GetAliasType(alias);
    if (!t.Type.IsGenericTypeDefinition) return t.Type;
    // recursively search for each argument
    t.UpdateArgs(typeArgs, args => BindToTypeAux(args.Current, args));
    return t.MakeType();
  }
}
