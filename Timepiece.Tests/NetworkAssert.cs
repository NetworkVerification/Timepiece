using System;
using Timepiece.Networks;
using Xunit;
using ZenLib;

namespace Timepiece.Tests;

/// <summary>
/// Helper assertions for checking annotated networks.
/// </summary>
public static class NetworkAssert
{
  /// <summary>
  /// Assert that the modular invariants hold (base, inductive, safety checks).
  /// </summary>
  /// <param name="net"></param>
  /// <typeparam name="T"></typeparam>
  /// <typeparam name="TV"></typeparam>
  /// <typeparam name="TS"></typeparam>
  public static void CheckSound<T, TV, TS>(AnnotatedNetwork<T, TV, TS> net)
  {
    Assert.Equal(Option.None<State<T, TV, TS>>(), net.CheckAnnotations());
  }

  /// <summary>
  /// Assert that the monolithic properties holds.
  /// </summary>
  /// <param name="net"></param>
  /// <typeparam name="T"></typeparam>
  /// <typeparam name="TV"></typeparam>
  /// <typeparam name="TS"></typeparam>
  public static void CheckSoundMonolithic<T, TV, TS>(AnnotatedNetwork<T, TV, TS> net)
  {
    Assert.Equal(Option.None<State<T, TV, TS>>(), net.CheckMonolithic());
  }

  /// <summary>
  /// Assert that the modular invariants do NOT hold.
  /// </summary>
  /// <param name="net"></param>
  /// <typeparam name="T"></typeparam>
  /// <typeparam name="TV"></typeparam>
  /// <typeparam name="TS"></typeparam>
  public static void CheckUnsound<T, TV, TS>(AnnotatedNetwork<T, TV, TS> net)
  {
    Assert.True(net.CheckAnnotations().HasValue);
  }

  /// <summary>
  /// Assert that a particular modular or monolithic check fails to hold.
  /// </summary>
  /// <param name="net"></param>
  /// <param name="check"></param>
  /// <typeparam name="T"></typeparam>
  /// <typeparam name="TV"></typeparam>
  /// <typeparam name="TS"></typeparam>
  /// <exception cref="ArgumentOutOfRangeException"></exception>
  public static void CheckUnsoundCheck<T, TV, TS>(AnnotatedNetwork<T, TV, TS> net, SmtCheck check)
  {
    Assert.True(check switch
    {
      SmtCheck.Monolithic => net.CheckMonolithic().HasValue,
      SmtCheck.Base => net.CheckBaseCase().HasValue,
      SmtCheck.Inductive => net.CheckInductive().HasValue,
      SmtCheck.Safety => net.CheckAssertions().HasValue,
      _ => throw new ArgumentOutOfRangeException(nameof(check), check, null)
    });
  }
}
