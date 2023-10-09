namespace Timepiece.Angler.Queries;

public enum NetworkQueryType
{
  Internet2BlockToExternal,
  Internet2NoMartians,
  Internet2NoTransit,
  FatReachable,
  FatPathLength,
  FatValleyFreedom,
  FatHijackFiltering,
}

public static class NetworkQueryTypeExtensions
{
  public static NetworkQueryType Parse(this string s)
  {
    return s switch
    {
      "bte" or "Internet2BlockToExternal" => NetworkQueryType.Internet2BlockToExternal,
      "mars" or "Internet2NoMartians" => NetworkQueryType.Internet2NoMartians,
      "transit" or "Internet2NoTransit" => NetworkQueryType.Internet2NoTransit,
      "fatReach" or "FatReachable" => NetworkQueryType.FatReachable,
      "fatLength" or "FatPathLength" => NetworkQueryType.FatPathLength,
      "fatValley" or "FatValleyFreedom" => NetworkQueryType.FatValleyFreedom,
      "fatHijack" or "FatHijackFiltering" => NetworkQueryType.FatHijackFiltering,
      _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Invalid network query type name! "
                                                               + "Acceptable values:\n"
                                                               + "- 'bte' or 'Internet2BlockToExternal' for 'Internet2BlockToExternal'"
                                                               + "- 'mars' or 'Internet2NoMartians' for 'Internet2NoMartians'"
                                                               + "- 'transit' or 'Internet2NoTransit' for 'Internet2NoTransit'"
                                                               + "- 'fatReach' or 'FatReachable' for 'FatReachable'"
                                                               + "- 'fatLength' or 'FatPathLength' for 'FatPathLength'"
                                                               + "- 'fatValley' or 'FatValleyFreedom' for 'FatValleyFreedom'"
                                                               + "- 'fatHijack' or 'FatHijackFiltering' for 'FatHijackFiltering'")
    };
  }
}
