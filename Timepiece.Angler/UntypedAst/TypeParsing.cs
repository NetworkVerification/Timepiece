using System.Numerics;
using Newtonsoft.Json;
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
  public static bool TryParse(string s, out TypeAlias? alias)
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
      "Bool" => new TypeAlias(typeof(ConstantExpr), typeof(bool)),
      "And" => new TypeAlias(typeof(BinaryOpExpr), typeof(bool)),
      "Or" => new TypeAlias(typeof(BinaryOpExpr), typeof(bool)),
      "Not" => new TypeAlias(typeof(Not)),
      "Havoc" => typeof(Havoc),
      // numeric expressions
      "Int32" => new TypeAlias(typeof(ConstantExpr)),
      "BigInt" => new TypeAlias(typeof(ConstantExpr)),
      "Uint32" => new TypeAlias(typeof(ConstantExpr)),
      "Plus" => new TypeAlias(typeof(Plus)),
      "LessThan" => new TypeAlias(typeof(BinaryOpExpr)),
      "LessThanEqual" => new TypeAlias(typeof(BinaryOpExpr)),
      "Equal" => new TypeAlias(typeof(BinaryOpExpr)),
      // pair expressions
      "Pair" => new TypeAlias(typeof(PairExpr), null, null),
      "First" => new TypeAlias(typeof(First), null, null),
      "Second" => new TypeAlias(typeof(Second), null, null),
      // option expressions
      "Some" => new TypeAlias(typeof(Some), (TypeAlias?) null),
      "None" => new TypeAlias(typeof(None), (TypeAlias?) null),
      // record expressions
      "GetField" => new TypeAlias(typeof(GetField), null, null),
      "WithField" => new TypeAlias(typeof(WithField), null, null),
      // set expressions
      "String" => new TypeAlias(typeof(ConstantExpr)),
      "SetContains" => new TypeAlias(typeof(SetContains)),
      "SetAdd" => new TypeAlias(typeof(SetAdd)),
      "EmptySet" => new TypeAlias(typeof(EmptySet)),
      "SetUnion" => new TypeAlias(typeof(SetUnion)),
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

public class TypeConverter : JsonConverter<TypeAlias>
{
  public override void WriteJson(JsonWriter writer, TypeAlias value, JsonSerializer serializer)
  {
    serializer.Serialize(writer, value.ToString());
  }

  public override TypeAlias ReadJson(JsonReader reader, Type objectType, TypeAlias existingValue, bool hasExistingValue,
    JsonSerializer serializer)
  {
    if (objectType != typeof(string)) throw new JsonSerializationException("Cannot read non-string as type");
    var s = (string?) reader.Value;
    if (s is null) throw new JsonException();
    if (TypeParsing.TryParse(s, out var alias) && alias.HasValue)
      return alias.Value;
    throw new JsonException($"Unable to deserialize TypeAlias from {s}");
  }
}
