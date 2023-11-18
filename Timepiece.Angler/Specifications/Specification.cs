namespace Timepiece.Angler.Specifications;

public enum Specification
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

public static class SpecificationExtensions
{
  public static Specification Parse(this string s)
  {
    return s switch
    {
      "bte" or "Internet2BlockToExternal" => Specification.Internet2BlockToExternal,
      "mars" or "Internet2NoMartians" => Specification.Internet2NoMartians,
      "private" or "Internet2NoPrivateAs" => Specification.Internet2NoPrivateAs,
      "reach" or "Internet2Reachable" => Specification.Internet2Reachable,
      "reachInternal" or "Internet2ReachableInternal" => Specification.Internet2ReachableInternal,
      "fatReach" or "FatReachable" => Specification.FatReachable,
      "fatLength" or "FatPathLength" => Specification.FatPathLength,
      "fatValley" or "FatValleyFreedom" => Specification.FatValleyFreedom,
      "fatHijack" or "FatHijackFiltering" => Specification.FatHijackFiltering,
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
