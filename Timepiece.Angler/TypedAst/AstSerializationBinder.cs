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
      "Return" => new TypeAlias(typeof(Return<>), new Type?[] {null}),
      "Assign" => new TypeAlias(typeof(Assign<>), new Type?[] {null}),
      "If" => new TypeAlias(typeof(IfThenElse<>), new Type?[] {null}),
      "Skip" => new TypeAlias(typeof(Skip)),
      "Seq" => new TypeAlias(typeof(AstStmt.Seq<>), new Type?[] {null}),
      // expressions
      "Var" => new TypeAlias(typeof(Var<>), new Type?[] {null}),
      // boolean expressions
      "Bool" => new TypeAlias(typeof(ConstantExpr<bool>)),
      "And" => new TypeAlias(typeof(And)),
      "Or" => new TypeAlias(typeof(Or)),
      "Not" => new TypeAlias(typeof(Not)),
      "Havoc" => new TypeAlias(typeof(Havoc)),
      // numeric expressions
      "Int32" => new TypeAlias(typeof(ConstantExpr<int>)),
      "BigInt" => new TypeAlias(typeof(ConstantExpr<BigInteger>)),
      "Uint32" => new TypeAlias(typeof(ConstantExpr<uint>)),
      "Plus" => new TypeAlias(typeof(Plus<>), new Type?[] {null}),
      "LessThan" => new TypeAlias(typeof(LessThan<>), new Type?[] {null}),
      "LessThanEqual" => new TypeAlias(typeof(LessThanEqual<>), new Type?[] {null}),
      "Equal" => new TypeAlias(typeof(Equal<>), new Type?[] {null}),
      // pair expressions
      "Pair" => new TypeAlias(typeof(PairExpr<,>), new Type?[] {null, null}),
      "First" => new TypeAlias(typeof(First<,>), new Type?[] {null, null}),
      "Second" => new TypeAlias(typeof(Second<,>), new Type?[] {null, null}),
      // option expressions
      "Some" => new TypeAlias(typeof(Some<>), new Type?[] {null}),
      "None" => new TypeAlias(typeof(None<>), new Type?[] {null}),
      // record expressions
      "GetField" => new TypeAlias(typeof(GetField<,>), new Type?[] {null, null}),
      "WithField" => new TypeAlias(typeof(WithField<,>), new Type?[] {null, null}),
      // set expressions
      "String" => new TypeAlias(typeof(ConstantExpr<string>)),
      "SetContains" => new TypeAlias(typeof(SetContains)),
      "SetAdd" => new TypeAlias(typeof(SetAdd)),
      "EmptySet" => new TypeAlias(typeof(EmptySet)),
      "SetUnion" => new TypeAlias(typeof(SetUnion)),
      // types
      "TRoute" => new TypeAlias(typeof(TRoute)),
      "TPair" => new TypeAlias(typeof(Pair<,>), new Type?[] {null, null}),
      "TOption" => new TypeAlias(typeof(Option<>), new Type?[] {null}),
      "TBool" => new TypeAlias(typeof(bool)),
      "TInt32" => new TypeAlias(typeof(int)),
      "TUint32" => new TypeAlias(typeof(uint)),
      "TTime" or "TBigInt" => new TypeAlias(typeof(BigInteger)),
      "TString" => new TypeAlias(typeof(string)),
      "TSet" => new TypeAlias(typeof(Set<string>)),
      "TUnit" => new TypeAlias(typeof(Unit)),
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
    return t.MakeGenericType();
  }
}

/// <summary>
///   A representation of a semi-open generic type and its arguments.
/// </summary>
internal readonly record struct TypeAlias
{
  /// <summary>
  ///   A representation of a semi-open generic type and its arguments.
  /// </summary>
  /// <param name="type">A (possibly-generic) Type.</param>
  /// <param name="args">
  ///   An array of arguments to this type, which should be of the same size
  ///   as the type's expected number of generic parameters.
  /// </param>
  /// <exception cref="ArgumentException">
  /// If the number of arguments is not the same as the expected number of arguments.
  /// </exception>
  public TypeAlias(Type type, Type?[] args)
  {
    if (type.IsGenericType && type.GetGenericArguments().Length != args.Length)
      throw new ArgumentException("Invalid type alias: number of generic arguments does not match type parameters.");

    Type = type;
    Args = args;
  }

  /// <summary>
  /// A TypeAlias for a closed type.
  /// </summary>
  /// <param name="type"></param>
  public TypeAlias(Type type)
  {
    Type = type;
    Args = Array.Empty<Type?>();
  }

  /// <summary>A (possibly-generic) Type.</summary>
  public Type Type { get; }

  /// <summary>
  ///   An array of arguments to this type, which should be of the same size
  ///   as the type's expected number of generic parameters.
  /// </summary>
  private Type?[] Args { get; }

  /// <summary>
  ///   Consume aliases from the given enumerator to fill in null arguments.
  /// </summary>
  /// <param name="typeAliases">An enumerator of string type aliases.</param>
  /// <param name="aliasLookup">A function to look up an alias string and potentially return a Type.</param>
  public void UpdateArgs(IEnumerator<string> typeAliases, Func<IEnumerator<string>, Type?> aliasLookup)
  {
    for (var i = 0; i < Args.Length; i++)
      if (Args[i] is null && typeAliases.MoveNext())
        Args[i] = aliasLookup(typeAliases);
  }

  public Type MakeGenericType()
  {
    if (Args.Any(t => t is null)) throw new ArgumentException("Not all arguments of TypeAlias are assigned.");

    return Type.MakeGenericType(Args!);
  }
}
