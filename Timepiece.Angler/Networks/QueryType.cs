namespace Timepiece.Angler.Networks;

public enum QueryType
{
  Internet2BlockToExternal,
  Internet2NoMartians,
  Internet2NoPrivateAs,
  Internet2Reachable,
  Internet2ReachableInternal,
  FatReachable,
  FatPathLength,
  FatValleyFreedom,
  FatHijackFiltering
}

public static class QueryTypeExtensions
{
  public static QueryType Parse(this string s)
  {
    return s switch
    {
      "bte" or "Internet2BlockToExternal" => QueryType.Internet2BlockToExternal,
      "mars" or "Internet2NoMartians" => QueryType.Internet2NoMartians,
      "private" or "Internet2NoPrivateAs" => QueryType.Internet2NoPrivateAs,
      "reach" or "Internet2Reachable" => QueryType.Internet2Reachable,
      "reachInternal" or "Internet2ReachableInternal" => QueryType.Internet2ReachableInternal,
      "fatReach" or "FatReachable" => QueryType.FatReachable,
      "fatLength" or "FatPathLength" => QueryType.FatPathLength,
      "fatValley" or "FatValleyFreedom" => QueryType.FatValleyFreedom,
      "fatHijack" or "FatHijackFiltering" => QueryType.FatHijackFiltering,
      _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Invalid network query type name! "
                                                               + "Acceptable values:\n"
                                                               + "- 'bte' or 'Internet2BlockToExternal' for 'Internet2BlockToExternal'"
                                                               + "- 'mars' or 'Internet2NoMartians' for 'Internet2NoMartians'"
                                                               + "- 'private' or 'Internet2NoPrivateAs' for 'Internet2NoPrivateAs'"
                                                               + "- 'reach' or 'Internet2Reachable' for 'Internet2Reachable'"
                                                               + "- 'reachInternal' or 'Internet2ReachableInternal' for 'Internet2ReachableInternal'"
                                                               + "- 'fatReach' or 'FatReachable' for 'FatReachable'"
                                                               + "- 'fatLength' or 'FatPathLength' for 'FatPathLength'"
                                                               + "- 'fatValley' or 'FatValleyFreedom' for 'FatValleyFreedom'"
                                                               + "- 'fatHijack' or 'FatHijackFiltering' for 'FatHijackFiltering'")
    };
  }
}
