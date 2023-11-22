using System.Text;

namespace Timepiece.Angler.Networks;

public enum QueryType
{
  Internet2BlockToExternal,
  Internet2BlockToExternalFaultTolerant,
  Internet2NoMartians,
  Internet2NoMartiansFaultTolerant,
  Internet2NoPrivateAs,
  Internet2Reachable,
  Internet2ReachableInternal,
  FatReachable,
  FatReachableAllToR,
  FatPathLength,
  FatPathLengthAllToR,
  FatValleyFreedom,
  FatValleyFreedomAllToR,
  FatHijackFiltering,
  FatHijackFilteringAllToR
}

public static class QueryTypeExtensions
{
  public static string ShortHand(this QueryType qt)
  {
    return qt switch
    {
      QueryType.Internet2BlockToExternal => "bte",
      QueryType.Internet2BlockToExternalFaultTolerant => "bteFT",
      QueryType.Internet2NoMartians => "mars",
      QueryType.Internet2NoMartiansFaultTolerant => "marsFT",
      QueryType.Internet2NoPrivateAs => "private",
      QueryType.Internet2Reachable => "reach",
      QueryType.Internet2ReachableInternal => "reachInternal",
      QueryType.FatReachable => "fatReach",
      QueryType.FatReachableAllToR => "fatReachAll",
      QueryType.FatPathLength => "fatLength",
      QueryType.FatPathLengthAllToR => "fatLengthAll",
      QueryType.FatValleyFreedom => "fatValley",
      QueryType.FatValleyFreedomAllToR => "fatValleyAll",
      QueryType.FatHijackFiltering => "fatHijack",
      QueryType.FatHijackFilteringAllToR => "fatHijackAll",
      _ => throw new ArgumentOutOfRangeException(nameof(qt), qt, $"Invalid {nameof(QueryType)} with no shorthand.")
    };
  }

  private static readonly Dictionary<string, QueryType> QueryNames = Enum.GetValues<QueryType>()
    .SelectMany(qt => new[] {(qt.ShortHand(), qt), ($"{qt}", qt)})
    .ToDictionary(p => p.Item1, p => p.Item2);

  internal static string AcceptableQueryValues()
  {
    var sb = new StringBuilder();
    sb.AppendLine("Acceptable values:");
    foreach (var qt in Enum.GetValues<QueryType>())
    {
      sb.AppendLine($"- '{qt.ShortHand()}' or '{qt}' for '{qt}'");
    }

    return sb.ToString();
  }

  public static QueryType ToQueryType(this string s)
  {
    return QueryNames.TryGetValue(s, out var queryType)
      ? queryType
      : throw new ArgumentOutOfRangeException(nameof(s), s,
        $"Invalid network query type name! {AcceptableQueryValues()}");
  }
}
