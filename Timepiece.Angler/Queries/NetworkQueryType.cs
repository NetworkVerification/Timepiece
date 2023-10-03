namespace Timepiece.Angler.Queries;

public enum NetworkQueryType
{
  Internet2BlockToExternal,
  Internet2NoMartians,
}

public static class NetworkQueryTypeExtensions
{
  public static NetworkQueryType Parse(this string s)
  {
    return s switch
    {
      "bte" or "Internet2BlockToExternal" => NetworkQueryType.Internet2BlockToExternal,
      "mars" or "Internet2NoMartians" => NetworkQueryType.Internet2NoMartians,
      _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Invalid network query type name! "
                                                               + "Acceptable values:\n"
                                                               + "- 'bte' or 'Internet2BlockToExternal' for 'Internet2BlockToExternal'"
                                                               + "- 'mars' or 'Internet2NoMartians' for 'Internet2NoMartians'")
    };
  }
}
