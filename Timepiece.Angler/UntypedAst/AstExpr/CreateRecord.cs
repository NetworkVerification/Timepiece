using Newtonsoft.Json;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class CreateRecord : Expr
{
  public Type RecordType { get; }
  public IDictionary<string, Expr> Fields { get; set; }

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

  public override void Rename(string oldVar, string newVar)
  {
    foreach (var fieldExpr in Fields.Values)
    {
      fieldExpr.Rename(oldVar, newVar);
    }
  }

  public (string, dynamic)[] GetFields(Func<Expr, dynamic> f)
  {
    return Fields.Select(field => (field.Key, f(field.Value))).ToArray();
  }
}
