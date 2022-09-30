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
    var fields = (ExprType) exprType! switch
    {
      ExprType.Var => new List<string> {"Name"},
      ExprType.Bool or ExprType.Int32 or ExprType.BigInt or ExprType.Uint32 or ExprType.String => new List<string>
        {"Value"},
      ExprType.And or ExprType.Or or ExprType.SetUnion => new List<string> {"Exprs"},
      ExprType.Plus or ExprType.LessThan or ExprType.LessThanEqual or ExprType.Equal or ExprType.SetAdd
        or ExprType.SetContains => new List<string> {"Operand1", "Operand2"},
      ExprType.Not => new List<string> {"Expr"},
      ExprType.Havoc => new List<string>(),
      ExprType.Pair => new List<string> {"First", "Second"},
      ExprType.First or ExprType.Second => new List<string> {"Pair"},
      ExprType.Some => new List<string> {"Expr"},
      ExprType.None => new List<string>(),
      ExprType.GetField => new List<string> {"Record", "FieldName"},
      ExprType.WithField => new List<string> {"Record", "FieldName", "FieldValue"},
      ExprType.EmptySet => new List<string>(),
      _ => throw new ArgumentOutOfRangeException()
    };

    var args = new List<dynamic>();
    while (reader.Read())
    {
      // TODO: figure out what fields we need

      Expr? expr = null;
      switch (reader.TokenType)
      {
        case JsonTokenType.EndObject:
          // TODO: construct the type from all collected information
          return exprAlias switch
          {
            _ => throw new NotImplementedException()
          };
        case JsonTokenType.PropertyName:
          propertyName = reader.GetString();
          reader.Read();
          switch (propertyName)
          {
            case "Value":
              // TODO: convert val based on the inner type of the ConstantExpr
              var ty = exprAlias.MakeType();
              if (ty == typeof(bool))
              {
                var val = reader.GetBoolean();
                args.Add(val);
              }
              else if (ty == typeof(int))
              {
                var val = reader.GetInt32();
                args.Add(val);
              }

              break;
            case "Expr":
              var e = Read(ref reader, typeof(Expr), options);
              throw new NotImplementedException();
            case "Operand1":
              throw new NotImplementedException();
            case "Operand2":
              throw new NotImplementedException();
          }

          break;
      }
    }

    throw new JsonException();
  }

  public override void Write(Utf8JsonWriter writer, Expr value, JsonSerializerOptions options)
  {
    throw new NotImplementedException("Serialization not implemented");
  }
}
