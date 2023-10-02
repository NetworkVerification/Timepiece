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
  /// Assert that the modular invariants hold (initial, inductive, safety checks).
  /// </summary>
  /// <param name="net"></param>
  /// <typeparam name="RouteType"></typeparam>
  /// <typeparam name="NodeType"></typeparam>
  public static void CheckSound<RouteType, NodeType>(AnnotatedNetwork<RouteType, NodeType> net)
  {
    Assert.Equal(Option.None<State<RouteType, NodeType>>(), net.CheckAnnotations());
  }

  public static void CheckSoundDelayed<RouteType, NodeType>(AnnotatedNetwork<RouteType, NodeType> net)
  {
    Assert.Equal(Option.None<State<RouteType, NodeType>>(), net.CheckAnnotationsDelayed());
  }

  /// <summary>
  /// Assert that the monolithic properties holds.
  /// </summary>
  /// <param name="net"></param>
  /// <typeparam name="RouteType"></typeparam>
  /// <typeparam name="NodeType"></typeparam>
  public static void CheckSoundMonolithic<RouteType, NodeType>(AnnotatedNetwork<RouteType, NodeType> net)
  {
    Assert.Equal(Option.None<State<RouteType, NodeType>>(), net.CheckMonolithic());
  }

  /// <summary>
  /// Assert that the modular invariants do NOT hold.
  /// </summary>
  /// <param name="net"></param>
  /// <typeparam name="RouteType"></typeparam>
  /// <typeparam name="NodeType"></typeparam>
  public static void CheckUnsound<RouteType, NodeType>(AnnotatedNetwork<RouteType, NodeType> net)
  {
    Assert.True(net.CheckAnnotations().HasValue);
  }

  public static void CheckSoundCheck<RouteType, NodeType>(AnnotatedNetwork<RouteType, NodeType> net, SmtCheck check)
  {
    Assert.Equal(Option.None<State<RouteType, NodeType>>(),
      check switch
      {
        SmtCheck.Monolithic => net.CheckMonolithic(),
        SmtCheck.Initial => net.CheckInitial(),
        SmtCheck.Inductive => net.CheckInductive(),
        SmtCheck.InductiveDelayed => net.CheckInductiveDelayed(),
        SmtCheck.Safety => net.CheckAssertions(),
        _ => throw new ArgumentOutOfRangeException(nameof(check), check, null)
      });
  }

  /// <summary>
  /// Assert that a particular modular or monolithic check fails to hold.
  /// </summary>
  /// <param name="net"></param>
  /// <param name="check"></param>
  /// <typeparam name="RouteType"></typeparam>
  /// <typeparam name="NodeType"></typeparam>
  /// <exception cref="ArgumentOutOfRangeException"></exception>
  public static void CheckUnsoundCheck<RouteType, NodeType>(AnnotatedNetwork<RouteType, NodeType> net, SmtCheck check)
  {
    Assert.True(check switch
    {
      SmtCheck.Monolithic => net.CheckMonolithic().HasValue,
      SmtCheck.Initial => net.CheckInitial().HasValue,
      SmtCheck.Inductive => net.CheckInductive().HasValue,
      SmtCheck.Safety => net.CheckAssertions().HasValue,
      SmtCheck.InductiveDelayed => net.CheckInductiveDelayed().HasValue,
      _ => throw new ArgumentOutOfRangeException(nameof(check), check, null)
    });
  }
}
