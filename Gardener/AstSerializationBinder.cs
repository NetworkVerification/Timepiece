using System.Numerics;
using System.Text.RegularExpressions;
using Gardener.AstExpr;
using Gardener.AstStmt;
using Newtonsoft.Json.Serialization;
using ZenLib;

namespace Gardener;

public class AstSerializationBinder<TRoute, TState> : ISerializationBinder
{
  private IDictionary<string, TypeAlias> AliasToType { get; }
  private IDictionary<string, TypeAlias> TyAliasToType { get; }

  private Type _state = typeof(TState);
  // TODO: dynamically choose which type to use based on the given typeName for bindToType
  private Type _timedState = typeof(Pair<TState, BigInteger>);

  public AstSerializationBinder()
  {
    // aliases to AST expressions and statements
    AliasToType = new Dictionary<string, TypeAlias>
    {
      {"Return", new TypeAlias(typeof(Return<>), new Type?[] {null})},
      {"Assign", new TypeAlias(typeof(Assign<>), new[] {_state})},
      {"If", new TypeAlias(typeof(IfThenElse<,>), new[] {null, _state})},
      {"Skip", new TypeAlias(typeof(Skip<>), new[] {_state})},
      {"Seq", new TypeAlias(typeof(Seq<,>), new[] {null, _state})},
      {"Var", new TypeAlias(typeof(Var<>), new Type?[] {null})},
      {"True", new TypeAlias(typeof(ConstantExpr<,>), new[] {typeof(bool), _state})},
      {"False", new TypeAlias(typeof(ConstantExpr<,>), new[] {typeof(bool), _state})},
      {"And", new TypeAlias(typeof(And<>), new[] {_state})},
      {"Or", new TypeAlias(typeof(Or<>), new[] {_state})},
      {"Not", new TypeAlias(typeof(Not<>), new[] {_state})},
      {"Havoc", new TypeAlias(typeof(Havoc<>), new[] {_state})},
      {"Int32", new TypeAlias(typeof(ConstantExpr<,>), new[] {typeof(int), _state})},
      {"Plus32", new TypeAlias(typeof(Plus<,>), new[] {typeof(int), _state})},
      {"Pair", new TypeAlias(typeof(PairExpr<,,>), new[] {null, null, _state})},
      {"First", new TypeAlias(typeof(First<,,>), new[] {null, null, _state})},
      {"Second", new TypeAlias(typeof(Second<,,>), new[] {null, null, _state})},
      // {"Some", new TypeAlias(typeof(Some<,>), new []{null, _state})},
      // {"None", new TypeAlias(typeof(None<,>), new []{null, _state})},
      {"GetField", new TypeAlias(typeof(GetField<,,>), new[] {null, null, _state})},
      {"WithField", new TypeAlias(typeof(WithField<,,>), new[] {null, null, _state})},
    };
    // aliases to Zen types
    TyAliasToType = new Dictionary<string, TypeAlias>
    {
      {"TRoute", new TypeAlias(typeof(TRoute), new Type?[] { })},
      {"TPair", new TypeAlias(typeof(Pair<,>), new Type?[] {null, null})},
      // {"RouteOption", typeof(Option<T>)},
      {"TBool", new TypeAlias(typeof(bool), new Type?[] { })},
      {"TInt32", new TypeAlias(typeof(int), new Type?[] { })},
      {"TTime", new TypeAlias(typeof(BigInteger), new Type[] { })},
      {"TString", new TypeAlias(typeof(string), new Type?[] { })},
      {"TSet", new TypeAlias(typeof(FBag<>), new[] {typeof(string)})},
      {"TUnit", new TypeAlias(typeof(Unit), new Type?[] { })},
    };
  }

  private static IEnumerable<string> ParseTypeArgs(string typeName)
  {
    var regex = new Regex(@"(?<term>\w+)");
    return regex.Matches(typeName).Select(match => match.Groups["term"].Value);
  }

  private Type? BindToTypeAux(string alias, IEnumerator<string> typeArgs)
  {
    if (AliasToType.ContainsKey(alias))
    {
      var t = AliasToType[alias];
      if (!t.Type.IsGenericTypeDefinition) return t.Type;
      // recursively search for each argument
      t.UpdateArgs(typeArgs, a => BindToTypeAux(a, typeArgs));
      return t.MakeGenericType();
    }

    if (TyAliasToType.ContainsKey(alias))
    {
      var t = TyAliasToType[alias];
      if (!t.Type.IsGenericTypeDefinition) return t.Type;
      t.UpdateArgs(typeArgs, a => BindToTypeAux(a, typeArgs));
      return t.MakeGenericType();
    }

    return null;
  }

  public Type BindToType(string? assemblyName, string typeName)
  {
    var timed = typeName.StartsWith("!");
    if (timed)
    {
      typeName = typeName.TrimStart('!');
    }

    var types = ParseTypeArgs(typeName).GetEnumerator();
    types.MoveNext();
    return BindToTypeAux(types.Current, types) ?? throw new ArgumentException($"Unable to bind {typeName}");
  }

  public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
  {
    assemblyName = null;
    typeName = serializedType.Name;
  }
}

/// <summary>
/// A representation of a semi-open generic type and its arguments.
/// </summary>
internal readonly record struct TypeAlias
{
  /// <summary>
  /// A representation of a semi-open generic type and its arguments.
  /// </summary>
  /// <param name="type">A (possibly-generic) Type.</param>
  /// <param name="args">
  /// An array of arguments to this type, which should be of the same size
  /// as the type's expected number of generic parameters.
  /// </param>
  public TypeAlias(Type type, Type?[] args)
  {
    if (type.IsGenericType && type.GetGenericArguments().Length != args.Length)
    {
      throw new ArgumentException("Invalid type alias: number of generic arguments does not match type parameters.");
    }

    Type = type;
    Args = args;
  }

  /// <summary>A (possibly-generic) Type.</summary>
  public Type Type { get; init; }

  /// <summary>
  /// An array of arguments to this type, which should be of the same size
  /// as the type's expected number of generic parameters.
  /// </summary>
  private Type?[] Args { get; init; }

  /// <summary>
  /// Consume aliases from the given enumerator to fill in null arguments.
  /// </summary>
  /// <param name="typeAliases">An enumerator of string type aliases.</param>
  /// <param name="aliasLookup">A function to look up an alias string and potentially return a Type.</param>
  public void UpdateArgs(IEnumerator<string> typeAliases, Func<string, Type?> aliasLookup)
  {
    for (var i = 0; i < Args.Length; i++)
    {
      if (Args[i] is null && typeAliases.MoveNext())
      {
        Args[i] = aliasLookup(typeAliases.Current);
      }
    }
  }

  public Type MakeGenericType()
  {
    if (Args.Any(t => t is null))
    {
      throw new ArgumentException("Not all arguments of TypeAlias are assigned.");
    }

    return Type.MakeGenericType(Args!);
  }
}
