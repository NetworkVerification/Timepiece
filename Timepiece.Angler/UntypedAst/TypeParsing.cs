using System.Numerics;
using Newtonsoft.Json;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstStmt;
using Timepiece.Datatypes;
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
    if (!TryParse(alias, out var t) || !t.HasValue)
      throw new ArgumentException($"{alias} not a valid type alias.", nameof(alias));
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
      "Call" => typeof(Call),
      // boolean expressions
      "Bool" => typeof(ConstantExpr),
      "And" => typeof(And),
      "Or" => typeof(Or),
      "Not" => typeof(Not),
      "Havoc" => typeof(Havoc),
      // numeric expressions
      "Int2" => new TypeAlias(typeof(ConstantExpr)),
      "Int32" => new TypeAlias(typeof(ConstantExpr)),
      "BigInt" => new TypeAlias(typeof(ConstantExpr)),
      "Uint32" => new TypeAlias(typeof(ConstantExpr)),
      "Plus32" => new TypeAlias(typeof(Plus)),
      "Sub32" => new TypeAlias(typeof(Sub)),
      "LessThan" => new TypeAlias(typeof(BinaryOpExpr)),
      "LessThanEqual" => new TypeAlias(typeof(BinaryOpExpr)),
      "Equals32" => new TypeAlias(typeof(Equals)),
      // pair expressions
      "Pair" => new TypeAlias(typeof(PairExpr), null, null),
      "First" => new TypeAlias(typeof(First), null, null),
      "Second" => new TypeAlias(typeof(Second), null, null),
      // option expressions
      "Some" => new TypeAlias(typeof(Some), (TypeAlias?) null),
      "None" => new TypeAlias(typeof(None), (TypeAlias?) null),
      // record expressions
      "GetField" => typeof(GetField),
      "WithField" => typeof(WithField),
      // set expressions
      "String" => typeof(ConstantExpr),
      "SetContains" => typeof(SetContains),
      "Subset" => typeof(Subset),
      "SetAdd" => new TypeAlias(typeof(SetAdd)),
      "LiteralSet" => new TypeAlias(typeof(LiteralSet)),
      "SetUnion" => new TypeAlias(typeof(SetUnion)),
      "SetRemove" => new TypeAlias(typeof(SetRemove)),
      // prefix expressions
      "PrefixContains" => typeof(PrefixContains),
      "PrefixSet" => typeof(LiteralSet),
      // types
      "TEnvironment" => typeof(RouteEnvironment),
      "TRoute" => typeof(BatfishBgpRoute),
      "TIpAddress" => typeof(uint),
      "TIpPrefix" => typeof(Ipv4Prefix),
      "TPrefixSet" => typeof(CSet<Ipv4Prefix>),
      "TPair" => new TypeAlias(typeof(Pair<,>), null, null),
      "TOption" => new TypeAlias(typeof(Option<>), (TypeAlias?) null),
      "TBool" => typeof(bool),
      "TInt32" => typeof(int),
      "TUint32" => typeof(uint),
      "TTime" or "TBigInt" => typeof(BigInteger),
      "TString" => typeof(string),
      "TSet" => typeof(CSet<string>),
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
