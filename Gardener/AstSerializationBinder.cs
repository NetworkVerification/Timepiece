using System.Numerics;
using System.Text.RegularExpressions;
using Gardener.AstExpr;
using Gardener.AstStmt;
using Newtonsoft.Json.Serialization;
using ZenLib;

namespace Gardener;

public class AstSerializationBinder<TRoute, TState> : ISerializationBinder
{
  private static readonly Type State = typeof(TState);

  private static readonly Type TimedState = typeof(Pair<TState, BigInteger>);

  /// <summary>
  /// Return a TypeAlias for the given AST expression, statement or type if it can be found,
  /// otherwise return null.
  /// </summary>
  /// <param name="alias">The given type name string to bind.</param>
  /// <param name="timed">Whether the TypeAlias is for a timed program state or an untimed state.</param>
  /// <returns>A TypeAlias if one exists, or null if not.</returns>
  private static TypeAlias? GetAliasType(string alias, bool timed)
  {
    var s = timed ? TimedState : State;
    return alias switch
    {
      "Return" => new TypeAlias(typeof(Return<>), new Type?[] {null}),
      "Assign" => new TypeAlias(typeof(Assign<>), new[] {s}),
      "If" => new TypeAlias(typeof(IfThenElse<,>), new[] {null, s}),
      "Skip" => new TypeAlias(typeof(Skip<>), new[] {s}),
      "Seq" => new TypeAlias(typeof(Seq<,>), new[] {null, s}),
      "Var" => new TypeAlias(typeof(Var<>), new Type?[] {null}),
      "True" => new TypeAlias(typeof(ConstantExpr<,>), new[] {typeof(bool), s}),
      "False" => new TypeAlias(typeof(ConstantExpr<,>), new[] {typeof(bool), s}),
      "And" => new TypeAlias(typeof(And<>), new[] {s}),
      "Or" => new TypeAlias(typeof(Or<>), new[] {s}),
      "Not" => new TypeAlias(typeof(Not<>), new[] {s}),
      "Havoc" => new TypeAlias(typeof(Havoc<>), new[] {s}),
      "Int32" => new TypeAlias(typeof(ConstantExpr<,>), new[] {typeof(int), s}),
      "Plus32" => new TypeAlias(typeof(Plus<,>), new[] {typeof(int), s}),
      "Pair" => new TypeAlias(typeof(PairExpr<,,>), new[] {null, null, s}),
      "First" => new TypeAlias(typeof(First<,,>), new[] {null, null, s}),
      "Second" => new TypeAlias(typeof(Second<,,>), new[] {null, null, s}),
      // "Some" => new TypeAlias(typeof(Some<,>), new []{null, s}),
      // "None" => new TypeAlias(typeof(None<,>), new []{null, s}),
      "GetField" => new TypeAlias(typeof(GetField<,,>), new[] {null, null, s}),
      "WithField" => new TypeAlias(typeof(WithField<,,>), new[] {null, null, s}),
      // types
      "TRoute" => new TypeAlias(typeof(TRoute), new Type?[] { }),
      "TPair" => new TypeAlias(typeof(Pair<,>), new Type?[] {null, null}),
      // "RouteOption" => typeof(Option<T>),
      "TBool" => new TypeAlias(typeof(bool), new Type?[] { }),
      "TInt32" => new TypeAlias(typeof(int), new Type?[] { }),
      "TTime" => new TypeAlias(typeof(BigInteger), new Type[] { }),
      "TString" => new TypeAlias(typeof(string), new Type?[] { }),
      "TSet" => new TypeAlias(typeof(FBag<>), new[] {typeof(string)}),
      "TUnit" => new TypeAlias(typeof(Unit), new Type?[] { }),
      _ => null
    };
  }


  private static IEnumerable<string> ParseTypeArgs(string typeName)
  {
    var regex = new Regex(@"(?<term>\w+)");
    return regex.Matches(typeName).Select(match => match.Groups["term"].Value);
  }

  private static Type? BindToTypeAux(string alias, IEnumerator<string> typeArgs, bool timed)
  {
    var t = GetAliasType(alias, timed);
    if (!t.HasValue) return null;
    if (!t.Value.Type.IsGenericTypeDefinition) return t.Value.Type;
    // recursively search for each argument
    t.Value.UpdateArgs(typeArgs, args => BindToTypeAux(args.Current, args, timed));
    return t.Value.MakeGenericType();
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
    return BindToTypeAux(types.Current, types, timed) ?? throw new ArgumentException($"Unable to bind {typeName}");
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
  public void UpdateArgs(IEnumerator<string> typeAliases, Func<IEnumerator<string>, Type?> aliasLookup)
  {
    for (var i = 0; i < Args.Length; i++)
    {
      if (Args[i] is null && typeAliases.MoveNext())
      {
        Args[i] = aliasLookup(typeAliases);
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
