using System;

namespace Timepiece;

public enum SmtCheck
{
  Monolithic,
  Initial,
  Inductive,
  InductiveDelayed,
  Safety,
  Modular,
  ModularDelayed,
}

public static class SmtCheckExtensions
{
  public static SmtCheck Parse(string s)
  {
    return s.ToLower() switch
    {
      "mono" or "monolithic" => SmtCheck.Monolithic,
      "init" or "initial" => SmtCheck.Initial,
      "ind" or "inductive" => SmtCheck.Inductive,
      "safe" or "safety" => SmtCheck.Safety,
      "mod" or "modular" => SmtCheck.Modular,
      "delay" or "delayed" => SmtCheck.ModularDelayed,
      "ind_delay" or "inductive_delayed" => SmtCheck.InductiveDelayed,
      _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Did not match any SmtCheck names")
    };
  }
}
