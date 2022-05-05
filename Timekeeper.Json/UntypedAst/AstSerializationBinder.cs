using System.Numerics;
using Newtonsoft.Json.Serialization;
using Timekeeper.Json.TypedAst.AstFunction;
using Timekeeper.Json.UntypedAst.AstExpr;
using ZenLib;
using Regex = System.Text.RegularExpressions.Regex;

namespace Timekeeper.Json.UntypedAst;

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
      "Return" => new TypeAlias(typeof(Return)),
      "Assign" => new TypeAlias(typeof(Assign)),
      "If" => new TypeAlias(typeof(IfThenElse)),
      // expressions
      "Var" => new TypeAlias(typeof(Var)),
      // boolean expressions
      "True" => new TypeAlias(typeof(ConstantExpr)),
      "False" => new TypeAlias(typeof(ConstantExpr)),
      "And" => new TypeAlias(typeof(BinaryOpExpr)),
      "Or" => new TypeAlias(typeof(BinaryOpExpr)),
      "Not" => new TypeAlias(typeof(BinaryOpExpr)),
      "Havoc" => new TypeAlias(typeof(Havoc)),
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
