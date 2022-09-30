using System.Numerics;
using ZenLib;
using static ZenLib.Zen;

namespace Timepiece.Datatypes
{
  [ZenObject]
  public struct Bgp
  {
    public Bgp(BigInteger lp, BigInteger asLength, CSet<string> tags)
    {
      Lp = lp;
      AsLength = asLength;
      Tags = tags;
    }

    public Bgp() : this(100, 0, new CSet<string>())
    {
    }

    /// <summary>
    /// Local preference of the given BGP announcement.
    /// </summary>
    public BigInteger Lp { get; set; }

    /// <summary>
    /// Abstract AS length of the given BGP announcement.
    /// </summary>
    public BigInteger AsLength { get; set; }

    /// <summary>
    /// List of community tags.
    /// </summary>
    public CSet<string> Tags { get; set; }

    public override string ToString()
    {
      var tagVal = string.Empty;
      foreach (var tag in Tags.Map.Values.Keys)
        if (string.IsNullOrEmpty(tagVal))
          tagVal += $"{tag}";
        else
          tagVal += $", {tag}";
      return $"Bgp(Lp={Lp},AsLength={AsLength},Tags=[{tagVal}])";
    }

    public static Zen<Bgp> Min(Zen<Bgp> b1, Zen<Bgp> b2)
    {
      return If(b1.GetLp() > b2.GetLp(), b1,
        If(b2.GetLp() > b1.GetLp(), b2,
          If(b1.GetAsLength() < b2.GetAsLength(), b1, b2)));
    }
  }

  public static class BgpExtensions
  {
    public static Zen<bool> HasTag(this Zen<Bgp> b, string tag)
    {
      return b.GetTags().Contains(tag);
    }

    public static Zen<Bgp> AddTag(this Zen<Bgp> b, string tag)
    {
      return b.WithTags(b.GetTags().Add(tag));
    }

    public static Zen<Bgp> IncrementAsLength(this Zen<Bgp> b)
    {
      return b.WithAsLength(b.GetAsLength() + BigInteger.One);
    }
  }
}
