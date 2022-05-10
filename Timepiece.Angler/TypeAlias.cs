namespace Timepiece.Angler;

/// <summary>
///   A representation of a semi-open generic type and its arguments.
/// </summary>
public readonly struct TypeAlias
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
  public TypeAlias(Type type, params TypeAlias?[] args)
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
    Args = Array.Empty<TypeAlias?>();
  }

  public static implicit operator TypeAlias(Type type)
  {
    return new TypeAlias(type);
  }

  /// <summary>A (possibly-generic) Type.</summary>
  public Type Type { get; }

  /// <summary>
  ///   An array of arguments to this type, which should be of the same size
  ///   as the type's expected number of generic parameters.
  /// </summary>
  private TypeAlias?[] Args { get; }

  /// <summary>
  ///   Consume aliases from the given enumerator to fill in null arguments.
  /// </summary>
  /// <param name="typeAliases">An enumerator of string type aliases.</param>
  /// <param name="aliasLookup">A function to look up an alias string and potentially return a Type.</param>
  public void UpdateArgs(IEnumerator<string> typeAliases, Func<IEnumerator<string>, TypeAlias?> aliasLookup)
  {
    for (var i = 0; i < Args.Length; i++)
      if (Args[i] is null && typeAliases.MoveNext())
        Args[i] = aliasLookup(typeAliases);
  }

  /// <summary>
  /// Create a fully-instantiated type from the TypeAlias.
  /// </summary>
  /// <returns>A Type.</returns>
  /// <exception cref="ArgumentException">If any of the type's generic arguments are unassigned.</exception>
  public Type MakeType()
  {
    if (!Type.IsGenericTypeDefinition) return Type;
    if (Args.Any(t => !t.HasValue)) throw new ArgumentException("Not all arguments of TypeAlias are assigned.");

    return Type.MakeGenericType(Args.Select(a => a!.Value.MakeType()).ToArray());
  }
}
