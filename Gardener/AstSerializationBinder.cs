using System.Text.RegularExpressions;
using Gardener.AstExpr;
using Gardener.AstStmt;
using Newtonsoft.Json.Serialization;
using ZenLib;

namespace Gardener;

public class AstSerializationBinder<T> : ISerializationBinder
{
  private readonly record struct TypeAlias(Type Type, Type?[] Args);

  private IDictionary<string, TypeAlias> AliasToType { get; }
  private IDictionary<string, Type> TyAliasToType { get; }

  public AstSerializationBinder()
  {
    // FIXME: figure out when to include T and when not
    AliasToType = new Dictionary<string, TypeAlias>
    {
      {"Return", new TypeAlias(typeof(Return<>), new[] {typeof(Pair<bool, T>)})},
      {"Assign", new TypeAlias(typeof(Assign<>), new[] {typeof(Pair<bool, T>)})},
      {"If", new TypeAlias(typeof(IfThenElse<,>), new[] {null, typeof(Pair<bool, T>)})},
      {"Skip", new TypeAlias(typeof(Skip<>), new[] {typeof(Pair<bool, T>)})},
      {"Seq", new TypeAlias(typeof(Seq<,>), new[] {null, typeof(Pair<bool, T>)})},
      {"Var", new TypeAlias(typeof(Var<>), new Type?[] {null})},
      {"True", new TypeAlias(typeof(ConstantExpr<,>), new[] {typeof(bool), typeof(Pair<bool, T>)})},
      {"False", new TypeAlias(typeof(ConstantExpr<,>), new[] {typeof(bool), typeof(Pair<bool, T>)})},
      {"And", new TypeAlias(typeof(And<>), new[] {typeof(Pair<bool, T>)})},
      {"Or", new TypeAlias(typeof(Or<>), new[] {typeof(Pair<bool, T>)})},
      {"Not", new TypeAlias(typeof(Not<>), new[] {typeof(Pair<bool, T>)})},
      {"Havoc", new TypeAlias(typeof(Havoc<>), new[] {typeof(Pair<bool, T>)})},
      {"Int32", new TypeAlias(typeof(ConstantExpr<,>), new[] {typeof(int), typeof(Pair<bool, T>)})},
      {"Plus32", new TypeAlias(typeof(Plus<,>), new[] {typeof(int), typeof(Pair<bool, T>)})},
      {"Pair", new TypeAlias(typeof(PairExpr<,,>), new[] {null, null, typeof(Pair<bool, T>)})},
      {"First", new TypeAlias(typeof(First<,,>), new[] {null, null, typeof(Pair<bool, T>)})},
      {"Second", new TypeAlias(typeof(Second<,,>), new[] {null, null, typeof(Pair<bool, T>)})},
      // {"Some", new TypeAlias(typeof(Some<,>), new []{null, typeof(Pair<bool, T>)})},
      // {"None", new TypeAlias(typeof(None<,>), new []{null, typeof(Pair<bool, T>)})},
      {"GetField", new TypeAlias(typeof(GetField<,,>), new[] {null, null, typeof(Pair<bool, T>)})},
      {"WithField", new TypeAlias(typeof(WithField<,,>), new[] {null, null, typeof(Pair<bool, T>)})},
    };
    TyAliasToType = new Dictionary<string, Type>
    {
      {"Route", typeof(T)},
      // {"RouteOption", typeof(Option<T>)},
      {"Bool", typeof(bool)},
      {"Int32", typeof(int)},
      {"String", typeof(string)},
      {"Set", typeof(FBag<string>)},
      {"Unit", typeof(Unit)},
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
      if (t.Type.IsGenericTypeDefinition)
      {
        // recursively search for each argument
        var args = new Type[t.Args.Length];
        for (var i = 0; i < t.Args.Length; i++)
        {
          // if we have more arguments, try and see if we can use one
          if (typeArgs.MoveNext())
          {
            var tt = BindToTypeAux(typeArgs.Current, typeArgs);
            if (tt is not null)
            {
              args[i] = tt;
              continue;
            }
          }
          // we reach this point if we failed to bind a type above
          if (t.Args[i] is not null)
          {
            args[i] = t.Args[i]!;
          }
          else
          {
            throw new ArgumentException($"Unable to bind {alias}");
          }
        }

        return t.Type.MakeGenericType(args);
      }

      return t.Type;
    }

    return TyAliasToType.ContainsKey(alias) ? TyAliasToType[alias] : null;
  }

  public Type BindToType(string? assemblyName, string typeName)
  {
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
