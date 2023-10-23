namespace Timepiece.Angler.Queries;

public enum NetworkQueryType
{
  Internet2BlockToExternal,
  Internet2NoMartians,
  Internet2GaoRexford,
  Internet2Reachable,
  Internet2ReachableInternal,
  FatReachable,
  FatPathLength,
  FatValleyFreedom,
  FatHijackFiltering
}

public static class NetworkQueryTypeExtensions
{
  public static NetworkQueryType Parse(this string s)
  {
    return s switch
    {
      "bte" or "Internet2BlockToExternal" => NetworkQueryType.Internet2BlockToExternal,
      "mars" or "Internet2NoMartians" => NetworkQueryType.Internet2NoMartians,
      "transit" or "Internet2GaoRexford" => NetworkQueryType.Internet2GaoRexford,
      "reach" or "Internet2Reachable" => NetworkQueryType.Internet2Reachable,
      "reachInternal" or "Internet2ReachableInternal" => NetworkQueryType.Internet2ReachableInternal,
      "fatReach" or "FatReachable" => NetworkQueryType.FatReachable,
      "fatLength" or "FatPathLength" => NetworkQueryType.FatPathLength,
      "fatValley" or "FatValleyFreedom" => NetworkQueryType.FatValleyFreedom,
      "fatHijack" or "FatHijackFiltering" => NetworkQueryType.FatHijackFiltering,
      _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Invalid network query type name! "
                                                               + "Acceptable values:\n"
                                                               + "- 'bte' or 'Internet2BlockToExternal' for 'Internet2BlockToExternal'"
                                                               + "- 'mars' or 'Internet2NoMartians' for 'Internet2NoMartians'"
                                                               + "- 'gaoRexford' or 'Internet2GaoRexford' for 'Internet2GaoRexford'"
                                                               + "- 'reach' or 'Internet2Reachable' for 'Internet2Reachable'"
                                                               + "- 'reachInternal' or 'Internet2ReachableInternal' for 'Internet2ReachableInternal'"
                                                               + "- 'fatReach' or 'FatReachable' for 'FatReachable'"
                                                               + "- 'fatLength' or 'FatPathLength' for 'FatPathLength'"
                                                               + "- 'fatValley' or 'FatValleyFreedom' for 'FatValleyFreedom'"
                                                               + "- 'fatHijack' or 'FatHijackFiltering' for 'FatHijackFiltering'")
    };
  }
}
