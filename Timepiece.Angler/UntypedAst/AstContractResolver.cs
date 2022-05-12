using Newtonsoft.Json.Serialization;

namespace Timepiece.Angler.UntypedAst;

public class AstContractResolver : DefaultContractResolver
{
  public static readonly AstContractResolver Instance = new();

  protected override JsonContract CreateContract(Type objectType)
  {
    var contract = base.CreateContract(objectType);
    if (objectType == typeof(Dictionary<string, object>))
    {
      contract.Converter = new ConstantsConverter();
    }

    return contract;
  }
}
