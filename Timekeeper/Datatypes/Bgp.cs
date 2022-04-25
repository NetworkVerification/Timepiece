using System.Numerics;
using ZenLib;
using static ZenLib.Zen;

namespace Timekeeper.Datatypes;

public record struct Bgp(BigInteger Lp, BigInteger AsLength, Set<string> Tags)
{
  public Bgp() : this(100, 0, new Set<string>())
  {
  }

  /// <summary>
  /// Local preference of the given BGP announcement.
  /// </summary>
  public BigInteger Lp { get; set; } = Lp;

  /// <summary>
  /// Abstract AS length of the given BGP announcement.
  /// </summary>
  public BigInteger AsLength { get; set; } = AsLength;

  /// <summary>
  /// List of community tags.
  /// </summary>
  public Set<string> Tags { get; set; } = Tags;

  public override string ToString()
  {
    var tagVal = string.Empty;
    foreach (var tag in Tags.Values.Values)
      if (string.IsNullOrEmpty(tagVal))
        tagVal += $"{tag}";
      else
        tagVal += $", {tag}";
    return $"Bgp(Lp={Lp},AsLength={AsLength},Tags=[{tagVal}])";
  }
}

public static class BgpExtensions
{
  public static Zen<BigInteger> GetLp(this Zen<Bgp> b)
  {
    return b.GetField<Bgp, BigInteger>("Lp");
  }

  public static Zen<BigInteger> GetAsLength(this Zen<Bgp> b)
  {
    return b.GetField<Bgp, BigInteger>("AsLength");
  }

  public static Zen<Set<string>> GetTags(this Zen<Bgp> b)
  {
    return b.GetField<Bgp, Set<string>>("Tags");
  }

  public static Zen<Bgp> SetLp(this Zen<Bgp> b, Zen<BigInteger> lp)
  {
    return b.WithField("Lp", lp);
  }

  public static Zen<Bgp> SetAsLength(this Zen<Bgp> b, Zen<BigInteger> cost)
  {
    return b.WithField("AsLength", cost);
  }

  public static Zen<Bgp> SetTags(this Zen<Bgp> b, Zen<Set<string>> tags)
  {
    return b.WithField("Tags", tags);
  }

  public static Zen<bool> HasTag(this Zen<Bgp> b, string tag)
  {
    return b.GetTags().Contains(tag);
  }

  public static Zen<Bgp> AddTag(this Zen<Bgp> b, string tag)
  {
    return b.SetTags(b.GetTags().Add(tag));
  }

  public static Zen<Bgp> IncrementAsLength(this Zen<Bgp> b)
  {
    return b.SetAsLength(b.GetAsLength() + BigInteger.One);
  }

  public static Zen<Bgp> Min(Zen<Bgp> b1, Zen<Bgp> b2)
  {
    return If(b1.GetLp() > b2.GetLp(), b1,
      If(b2.GetLp() > b1.GetLp(), b2,
        If(b1.GetAsLength() < b2.GetAsLength(), b1, b2)));
  }
}