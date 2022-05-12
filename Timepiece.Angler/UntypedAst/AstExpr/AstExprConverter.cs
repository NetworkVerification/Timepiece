using System.Text.Json;
using System.Text.Json.Serialization;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class AstExprConverter : JsonConverter<Expr>
{
  private enum ExprType
  {
    Var,
    Bool,
    And,
    Or,
    Not,
    Havoc,
    Int32,
    BigInt,
    Uint32,
    Plus,
    LessThan,
    LessThanEqual,
    Equal,
    Pair,
    First,
    Second,
    Some,
    None,
    GetField,
    WithField,
    String,
    SetContains,
    SetAdd,
    EmptySet,
    SetUnion,
  }

  public override Expr? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    if (reader.TokenType != JsonTokenType.StartObject)
    {
      throw new JsonException();
    }

    reader.Read();
    if (reader.TokenType != JsonTokenType.PropertyName)
    {
      throw new JsonException();
    }

    var propertyName = reader.GetString();
    if (propertyName != "$type")
    {
      throw new JsonException();
    }

    reader.Read();
    if (reader.TokenType != JsonTokenType.Number)
    {
      throw new JsonException();
    }

    var typeInfo = reader.GetString()!;
    var exprAlias = TypeParsing.ParseType(typeInfo);
    if (!Enum.TryParse(typeof(ExprType), typeInfo, true, out var exprType)) throw new JsonException();
    while (reader.Read())
    {
      // TODO: figure out what fields we need
      switch ((ExprType) exprType!)
      {
        case ExprType.Var:
        case ExprType.Bool:
        case ExprType.And:
        case ExprType.Or:
        case ExprType.Not:
        case ExprType.Havoc:
        case ExprType.Int32:
        case ExprType.BigInt:
        case ExprType.Uint32:
        case ExprType.Plus:
        case ExprType.LessThan:
        case ExprType.LessThanEqual:
        case ExprType.Equal:
        case ExprType.Pair:
          // var firstType = exprAlias.Args[0]!.Value.Type;
          // var secondType = exprAlias.Args[1]!.Value.Type;
          break;
        case ExprType.First:
          // var firstType = exprAlias.Args[0]!.Value.Type;
          // var secondType = exprAlias.Args[1]!.Value.Type;
          break;
        case ExprType.Second:
          // var firstType = exprAlias.Args[0]!.Value.Type;
          // var secondType = exprAlias.Args[1]!.Value.Type;
          break;
        case ExprType.Some:
        case ExprType.None:
        case ExprType.GetField:
        case ExprType.WithField:
        case ExprType.String:
        case ExprType.SetContains:
        case ExprType.SetAdd:
        case ExprType.EmptySet:
        case ExprType.SetUnion:
        default:
          break;
      }

      switch (reader.TokenType)
      {
        case JsonTokenType.EndObject:
          // TODO: construct the type from all collected information
          return null;
        case JsonTokenType.PropertyName:
          propertyName = reader.GetString();
          reader.Read();
          switch (propertyName)
          {
            case "Value":
              var val = reader.GetString();
              // TODO: convert val based on the inner type of the ConstantExpr
              return exprAlias.MakeType() switch
              {
                _ => throw new ArgumentOutOfRangeException()
              };
              // ((ConstantExpr)expr).value = val;
              break;
            case "Expr":
              throw new NotImplementedException();
            case "Expr1":
              throw new NotImplementedException();
            case "Expr2":
              throw new NotImplementedException();
          }

          break;
      }
    }

    throw new JsonException();
  }

  public override void Write(Utf8JsonWriter writer, Expr value, JsonSerializerOptions options)
  {
    writer.WriteStartObject();

    switch (value)
    {
      case BinaryOpExpr binaryOpExpr:
        break;
      case Call call:
        break;
      case ConstantExpr constantExpr:
        break;
      case Havoc:
        writer.WriteString("$type", "Havoc");
        break;
      case None none:
        var ty = none.innerType.ToString();
        writer.WriteString("$type", $"None({ty})");
        break;
      case UnaryOpExpr unaryOpExpr:
        break;
      case Var var:
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(value));
    }

    writer.WriteEndObject();
  }
}
