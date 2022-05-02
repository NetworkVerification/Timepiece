using Newtonsoft.Json.Serialization;
using Timekeeper.Json.TypedAst;

namespace Timekeeper.Json;

public class AstContractResolver<TState> : DefaultContractResolver
{
  public static readonly AstContractResolver<TState> Instance = new();

  protected override JsonContract CreateContract(Type objectType)
  {
    var contract = base.CreateContract(objectType);
    if (objectType == typeof(Dictionary<string, object>))
    {
      contract.Converter = new ConstantsConverter<TState>();
    }

    return contract;
  }
}
