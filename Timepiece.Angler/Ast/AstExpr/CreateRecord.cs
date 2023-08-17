using Newtonsoft.Json;

namespace Timepiece.Angler.Ast.AstExpr;

public class CreateRecord : Expr
{
  [JsonConstructor]
  public CreateRecord(string recordType, IDictionary<string, Expr> fields) : this(
    TypeParsing.ParseType(recordType).MakeType(), fields)
  {
  }

  public CreateRecord(Type recordType, IDictionary<string, Expr> fields)
  {
    RecordType = recordType;
    Fields = fields;
  }

  public Type RecordType { get; }
  public IDictionary<string, Expr> Fields { get; set; }

  // TODO(tim): for now, we ignore the DefaultPolicy
  // this is a hacky design, to say the least, so it should be replaced
  public static string[] SkippedFields { get; set; } = {"DefaultPolicy"};

  public override void Rename(string oldVar, string newVar)
  {
    foreach (var fieldExpr in Fields.Values) fieldExpr.Rename(oldVar, newVar);
  }

  public (string, dynamic)[] GetFields(Func<Expr, dynamic> f)
  {
    return Fields
      // skip any fields in the SkippedFields array
      .Where(field => !SkippedFields.Contains(field.Key))
      .Select(field => (field.Key, f(field.Value)))
      .ToArray();
  }
}
