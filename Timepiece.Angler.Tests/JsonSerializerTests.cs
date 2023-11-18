using Newtonsoft.Json;
using Timepiece.Angler.DataTypes;
using Timepiece.DataTypes;
using ZenLib;

namespace Timepiece.Angler.Tests;

public static class JsonSerializerTests
{
  [Fact]
  public static void DeserializeIpv4Wildcard()
  {
    const string ipv4WildcardText = @"{
      'Begin': '64.57.23.32',
      'HostMask': '0.0.0.31'
    }";
    var ipv4Wildcard = new Ipv4Wildcard("64.57.23.32", "0.0.0.31");
    var deserialized = JsonConvert.DeserializeObject<Ipv4Wildcard>(ipv4WildcardText);
    Assert.Equivalent(ipv4Wildcard, deserialized);
  }

  [Fact]
  public static void DeserializeRouteFilterLine()
  {
    const string routeFilterText = @"{
      'Action': true,
      'MaxLength': 32,
      'MinLength': 27,
      'Wildcard': {
        'Begin': '64.57.23.32',
        'HostMask': '0.0.0.31'
      }
    }";
    var routeFilterLine = new RouteFilterLine(true, new Ipv4Wildcard("64.57.23.32", "0.0.0.31"), new UInt<_6>(27),
      new UInt<_6>(32));
    var deserialized = JsonConvert.DeserializeObject<RouteFilterLine>(routeFilterText);
    Assert.Equivalent(routeFilterLine, deserialized);
  }

  [Fact]
  public static void DeserializeRouteFilterList()
  {
    const string routeFilterListText = @"{
      '$type': 'RouteFilterList',
      'Lines': [
        {
          'Action': true,
          'MaxLength': 32,
          'MinLength': 27,
          'Wildcard': {
            'Begin': '64.57.23.32',
            'HostMask': '0.0.0.31'
          }
        },
        {
          'Action': true,
          'MaxLength': 32,
          'MinLength': 24,
          'Wildcard': {
            'Begin': '64.57.31.0',
            'HostMask': '0.0.0.255'
          }
        },
      ]
    }";
    var rfl = new RouteFilterList(new RouteFilterLine[]
    {
      new(true, new Ipv4Wildcard("64.57.23.32", "0.0.0.31"), new UInt<_6>(27), new UInt<_6>(32)),
      new(true, new Ipv4Wildcard("64.57.31.0", "0.0.0.255"), new UInt<_6>(24), new UInt<_6>(32))
    });
    var deserialize = JsonConvert.DeserializeObject<RouteFilterList>(routeFilterListText);
    Assert.Equivalent(rfl, deserialize);
  }
}
