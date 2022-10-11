using Newtonsoft.Json;
using ZenLib;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public class GetField : UnaryOpExpr
{
  private static GenericMethod GetMethod(Type recordTy, Type fieldTy) =>
    new(typeof(Zen), "GetField", recordTy, fieldTy);

  public GetField(Type recordTy, Type fieldTy, Expr record, string fieldName) : base(record,
    r => GetMethod(recordTy, fieldTy).Call(r, fieldName))
  {
  }

  [JsonConstructor]
  public GetField(string recordType, string fieldType, Expr record, string fieldName) : this(
    TypeParsing.ParseType(recordType).MakeType(), TypeParsing.ParseType(fieldType).MakeType(), record, fieldName) {}
}
