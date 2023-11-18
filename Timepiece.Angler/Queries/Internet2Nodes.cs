namespace Timepiece.Angler.Queries;

public static class Internet2Nodes
{
  /// <summary>
  ///   The nodes of Internet2's AS.
  /// </summary>
  public static readonly string[] AsNodes =
    {"atla-re1", "chic", "clev-re1", "hous", "kans-re1", "losa", "newy-re1", "salt-re1", "seat-re1", "wash"};

  /// <summary>
  ///   Addresses for neighbors in the OTHER-INTERNAL, PAIX and WILC peer group of the internal nodes.
  ///   These connections should also be considered internal.
  /// </summary>
  public static readonly string[] OtherInternalNodes =
  {
    // OTHER-INTERNAL peer group
    "64.57.16.133", "64.57.16.196", "64.57.16.4", "64.57.16.68", "64.57.17.133", "64.57.17.194",
    "64.57.17.7", "64.57.17.71", "64.57.19.2",
    "64.57.28.251", // PAIX group (Palo Alto Internet eXchange)
    "64.57.28.252" // WILC group
  };

  /// <summary>
  ///   Addresses for neighbors in the OTHER-INTERNAL peer group.
  ///   These connections should be considered internal and any routes from them are rejected.
  /// </summary>
  public static readonly string[] OtherInternalGroup =
  {
    "64.57.16.133", "64.57.16.196", "64.57.16.4", "64.57.16.68", "64.57.17.133", "64.57.17.194",
    "64.57.17.7", "64.57.17.71", "64.57.19.2",
  };

  public static readonly string[] OtherGroup =
  {
    "128.223.51.102", "128.223.51.108", "207.75.164.233", "207.75.164.213",
    "203.181.248.35" // "zebra.jp.apan.net | I2-S13129"
  };

  /// <summary>
  ///   Addresses for the AL2S_MGMT peer group.
  ///   See https://internet2.edu/services/layer-2-service/ for what AL2S is.
  ///   Routes should never be imported or exported to these nodes.
  /// </summary>
  public static readonly string[] AdvancedLayer2ServiceManagementGroup =
  {
    "64.57.25.164", "64.57.25.165", "64.57.24.204", "64.57.24.205", "64.57.25.124", "64.57.25.236"
  };

  public static readonly string[] AtlaConnectorNodes =
  {
    "149.165.254.20", // "[RE] Indiana Gigapop | I2-S12522"
    "198.32.252.237", // "[RE] FIU/AMPATH via SOX | I2-S12836"
    "216.249.136.197", // "[RE] KyRON | I2-S09372"
    "205.233.255.36", // "[RE] MissION | I2-S12530"
    "143.215.193.3", // "[RE] SoX via AL2S/ATLA | I2-S12541"
    "108.59.25.20", // "[RE] FLR via AL2S/JACK | I2-S12519"
    "198.71.46.170", // "[RE] MCNC R&E; via AL2S | I2-S12527"
    "149.165.128.12", // "[RE] Indiana Gigapop via AL2S | I2-S12522"
    // inactive: "206.196.177.76", // "[RE] MAX (Mid Atlantic Crossroads) via AL2S/WASH | [PENDING][6833:121] | I2-S12528"
  };

  public static readonly string[] ChicConnectorNodes =
  {
    "205.213.118.5", // "[RE] WiscRen | I2-S12838"
    "192.122.183.29", // "[RE] Merit via MREN via CIC | I2-S12839"
    "198.49.182.4", // "[RE] U of Iowa via CIC | I2-S12840"
    "72.36.127.157", // "[RE] UIUC via CIC | I2-S12841"
    "205.213.119.9", // "[RE] WiscREN-EQCH 10G via CIC | I2-S12838"
    "192.122.183.45", // "[RE] MERIT via CIC | I2-S12842"
    "128.135.247.126", // "[RE] UOC via CIC | I2-S12847"
    "72.36.127.161", // "[RE] UIUC via CIC Backup | I2-S12841"
    "146.57.253.53", // "[RE] NorthernLights Gigapop via CIC, 10G | I2-S12848"
    "141.225.250.25", // "[RE] University of Memphis | I2-S12544"
    "192.122.183.97", // "[RE] MERIT | I2-S12842"
    "143.215.193.14", // "[RE] SOX | I2-S12541"
    "192.5.143.28", // "[RE] Northwestern U via CIC | I2-S12849"
    "149.165.254.185", // "[RE] Indiana Gigapop via AL2S | I2-S12522"
    "72.36.127.181", // "[RE] UIUC via AL2S | I2-S12850"
    "164.113.255.249", // "[RE] GPN back-up | I2-S12520"
    "164.113.255.245", // "[RE] GPN back-up | I2-S12520"
    "144.92.254.228", // "[RE] University of Wisconsin-Madison via CIC 100G | I2-S12851"
    "149.165.254.109", // "[RE] Indiana Gigapop via CIC | I2-S12522 | [NO-MONITOR] | [3546:144]"
    "137.164.26.48", // "[RE] CENIC via AL2S/STAR | I2-S12514"
    "192.43.217.221", // "[RE] FRGP multicast via AL2S/STAR | I2-S12852"
    "192.43.217.223", // "[RE] FRGP unicast AL2S/STAR | I2-S12852"
    "143.215.193.37", // "[RE] Sox via AL2S/ATLA | I2-S12541"
    "192.170.225.5", // "[RE] UOC via CIC | I2-S12847"
    "198.71.45.232", // "[RE]University of Michigan via CIC/AL2S | I2-S12853"
    "198.71.45.234", // "[RE] MREN via AL2S | I2-S12526"
    "198.71.46.183", // "[RE] University of Wisconsin System Network (Madison) via CIC OMniPOP | I2-S51597"
    "192.73.48.23", // "[RE] UMontana via AL2S/MISS2 | I2-S12952"
    "146.57.253.41", // "[RE] NorthernLights Gigapop via CIC | I2-S12848"
    "192.88.192.141", // "[RE] OARnet via Merit | I2-S12534"
    "198.71.46.220", // "[RE]UNMviaAL2S/STAR | I2-S11506 "
    "198.71.46.222", // "[RE] OARnet multicast via AL2S cinc | I2-S12678"
    "198.71.46.0", // "[RE] OARnet unicast via AL2S cinc | I2-S12534"
    "199.109.11.37", // "[RE] NYSERNet | I2-S12533"
    "198.71.45.153", // "[RE] PNWGP via AL2S/SEAT | I2-S12536"
    "146.57.255.244", // "[RE] NorthernLights Gigapop via AL2S | I2-S12848"
    "192.5.89.253", // "[RE] NOX via AL2S/ALBA | I2-S12532 [PENDING] [472:144]"
    "198.71.45.157", // "[RE] PSU via CIC | I2-S54971 | [PENDING][4680:144]"
    "64.57.28.2", // "[RE] NCSA via AL2S/CHIC | I2-S54972"
  };

  public static readonly string[] ClevConnectorNodes =
  {
    "192.5.89.17", // "[RE] NOX via AL2S/BOST | I2-S12532"
    "192.88.115.24", // "[RE] 3ROX | I2-S12542"
    "192.88.115.80", // "[RE] 3ROX via AL2S/PITT | I2-S12542"
    "198.71.46.15", // "[RE] Smithsonian via AL2S/ASHB | I2-S12540"
    "192.122.175.12", // "[RE] MARIA via AL2S/ASHB | I2-S12529"
    "132.198.255.161", // "[RE] UVM via AL2S/ALBA | I2-S12861"
    "129.170.9.242", // "[RE] Dartmouth via AL2S | I2-S12862"
    "198.71.45.248", // "[RE] University of Michigan via CIC/AL2S | I2-S12853"
    "198.71.46.152", // "[RE] NIH via AL2S/ASHB | I2-S50449"
    "198.71.46.191", // "[RE] GE Research via AL2S/ALBA | I2-S25284"
    "198.71.46.156", // "[RE] NLM via AL2S/ASHB | I2-S50448"
    "198.71.46.212", // "[RE] University of Wisconsin System Network (Milwaukee) via CIC OMniPOP | I2-S51597"
    "198.71.46.219", // "[RE] CEN via AL2S/HART2 | I2-S49806"
    "64.57.30.59", // "[RE] BIOGEN via AL2S/BOST | I2-S51219 [PENDING][5077:144]"
    "198.71.46.240", // "[RE] USDA via AL2S | I2-S50731"
    "130.111.0.84", // "[RE] UMS via AL2S/ALBA | I2-S53461"
    "149.165.254.109", // "[RE] Indiana Gigapop via AL2S/STAR | I2-S12522"
    "199.109.11.33", // "[RE] NYSERNet | I2-S12533"
    "146.57.255.250", // "[RE] NorthernLights Gigapop via AL2S | I2-S12848"
    // inactive: "206.196.177.4", // "[RE] MAX (Mid Atlantic Crossroads) via AL2S/ASHB | [PENDING][6833:121] | I2-S12528"
  };

  public static readonly string[] HousConnectorNodes =
  {
    "205.233.255.32", // "[RE] MissION | I2-S12530"
    "164.58.245.245", // "[RE] OneNet via sdn-sw.tuls | I2-S12864"
    "208.90.110.96", // "[RE] AREON via AL2S/TULS | I2-S12865"
    "74.200.187.10", // "[RE] LEARN via AL2S/HOUH | I2-S12523"
    "74.200.187.33", // "[RE] LEARN via AL2S/DALL | I2-S12523"
    "137.164.26.204", // "[RE] CENIC (CalREN-HPR) via AL2S/LOSA | I2-S12514"
    "108.59.26.20", // "[RE] FLR via AL2S/Jack | I2-S12519"
    "198.71.46.75", // "[RE] Sun Corridor via AL2S/TUCS | I2-S12543"
    "208.100.127.1", // "[RE] LONI v4 via AL2S/BATO | I2-S12524"
    "198.71.46.162", // "[RE] Sun Corridor via AL2S/PHOE | I2-S12543"
  };

  public static readonly string[] KansConnectorNodes =
  {
    "64.57.28.178", // "[RE] KanREN via GPN | I2-S12869"
    "146.57.253.57", // "[RE] UMN/NL via GPN | I2-S12848"
    "164.113.255.253", // "[RE] GPN via AL2S | I2-S12520"
    "164.58.7.49", // "[RE] OneNet via sdn-sw.tuls | I2-S12864"
    "208.90.110.98", // "[RE] AREON via AL2S/TULS | I2-S12865"
    "74.200.187.37", // "[RE] LEARN via AL2S/DALL | I2-S12523"
    "198.71.46.86", // "[RE] GlobalSummit via AL2S/KANS | I2-S20098"
    "74.200.187.54", // "[RE] LEARN via AL2S/HOUH | I2-S12523"
    "146.57.255.248", // "[RE] UNM NorthernLights via AL2S | I2-S12848"
  };

  public static readonly string[] LosaConnectorNodes =
  {
    "137.164.26.133", // "[RE] CENIC (CalREN-HPR) | I2-S12514"
    "198.32.165.65", // "[RE] Oregon Gigapop via DWS | I2-S12535"
    "205.166.205.12", // "[RE] U of Hawaii Layer 2 | I2-S12948"
    "137.164.26.200", // "[RE] CENIC (CalREN-HPR) via AL2S | I2-S12514"
    "140.197.253.143", // "[RE] UEN via AL2S/SALT | I2-S12705"
    "198.71.46.73", // "[RE] Sun Corridor via AL2S/TUCS | I2-S12543"
    "198.71.46.160", // "[RE] Sun Corridor via AL2S/PHOE | I2-S12543"
    "192.133.159.149", // "[RE] CSN via PacWave | [PENDING][3803:144]"
    "207.197.17.77", // "[RE] NSHE via AL2S | I2-S54033"
  };

  public static readonly string[] NewyConnectorNodes =
  {
    "192.5.89.221", // "[RE] NOX | I2-S12532"
    "206.196.177.50", // "[RE] MAX (Mid-Atlantic CrossRoads) | I2-S12528"
    "198.71.45.245", // "[RE] CAAREN via AL2S/WASH | I2-S12513"
    "128.91.240.177", // "[RE] UPENN via AL2S/PHIL | I2-S12949"
    "198.71.46.189", // "[RE] GE Research via AL2S/ALBA | I2-S25284"
    "216.27.100.5", // "[RE] MAGPI via AL2S | I2-S12525"
    "199.109.5.1", // "[RE] NYSERNet | I2-S12533"
    "198.71.46.215", // "[RE]CEN v4 via AL2S/HART2 | I2-S49806"
    "64.57.30.57", // "[RE] Biogenisis via AL2S/BOST | I2-S51219"
  };

  public static readonly string[] SaltConnectorNodes =
  {
    "64.57.28.207", // "[RE] IRON (Idaho Regional Optical Network) | I2-S12950"
    "198.71.45.231", // "[RE] UEN via AL2S/SALT | I2-S12705"
    "198.71.46.84", // "[RE] GlobalSummit via AL2S/KANS | I2-S20098"
    "207.197.33.33", // "[RE] NSHE via AL2S | I2-S54033"
    "207.197.17.73", // "[RE] NSHE via AL2S | I2-S54033"
  };

  public static readonly string[] SeatConnectorNodes =
  {
    "137.164.26.141", // "[RE] CENIC via LAX-DC | I2-S12514"
    "207.231.240.7", // "[RE] Microsoft via PAC Wave vlan706 | I2-S12951"
    "207.231.241.7", // "[RE] Microsoft via Pac Wave vlan776 | I2-S12951"
    "64.57.28.54", // "[RE] PNWGP | I2-S12536"
    "198.32.163.69", // "[RE] Oregon Gigapop via SEAT via Internet DWS I2-PORT-SEAT-I2-05681 | I2-S12535"
    "205.166.205.10", // "[RE] U of Hawaii (Layer 2) via PNWGP | I2-S12948"
    "198.71.46.165", // "[RE]Layer 2 Participant UW Science DMZ v4 via PNWGP AL2S/SEAT | I2-S14320"
    "192.73.48.17", // "[RE] UMontana via AL2S/MISS2 | I2-S12952"
    "198.71.46.246", // "[RE] FRGP via AL2S/SEAT | I2-S12852"
    "198.71.44.238", // " Singapore peering vlan 4012 | I2-S54543"
    "207.197.33.37", // "[RE] NSHE via AL2S | I2-S54033"
    "146.57.255.240", // "[RE] NorthernLights Gigapop via AL2S | I2-S12848"
  };

  public static readonly string[] WashConnectorNodes =
  {
    "206.196.177.105", // "[RE] MAX backup via NGIX-East | I2-S12528"
    "205.186.224.49", // "[RE] C-Light/SCLR | I2-S12953"
    "192.88.192.237", // "[RE] OARnet via AL2S | I2-S12534"
    "199.18.156.249", // "[RE] OARnet multicast via AL2S | I2-S12678"
    "64.57.23.30", // "[RE] World Bank | I2-S12954"
    "192.122.183.13", // "[RE] MERIT via OARnet/AL2S | I2-S12842"
    "198.71.45.228", // "[RE] MAX (Mid Atlantic Crossroads) via AL2S/WASH | I2-S12528"
    "192.88.115.82", // "[RE] 3ROX via AL2S/PITT | I2-S12542"
    "198.71.46.13", // "[RE] Smithsonian via AL2S/ASHB | I2-S12540"
    "198.71.45.247", // "[RE] CAAREN via AL2S/WASH | I2-S12513"
    "192.122.175.14", // "[RE] MARIA via AL2S/ASHB | I2-S12529"
    "128.91.240.181", // "[RE] UPENN via AL2S/PHIL | I2-S12949"
    "198.71.46.154", // "[RE] NIH via AL2S/ASHB | I2-S50449"
    "198.71.46.158", // "[RE] NLM via AL2S/ASHB | I2-S50448"
    "198.71.46.186", // "[RE] MCNC via AL2S | I2-S12527"
    "198.71.46.207", // "[RE] MREN via AL2S | I2-S12526"
    "216.27.100.17", // "[RE] MAGPI via AL2S | I2-S12525"
    "199.109.5.25", // "[RE] NYSERNet | I2-S12533"
    "204.238.76.33", // "[RE] Drexel University | I2-S12517"
    // inactive: "206.196.177.2", // "[RE] MAX (Mid Atlantic Crossroads) via AL2S/ASHB | [PENDING][6833:121] | I2-S12528"
    // inactive: "206.196.177.78", // "[RE] MAX (Mid Atlantic Crossroads) via AL2S/WASH | [PENDING][6833:121] | I2-S12528"
  };

  public static readonly IEnumerable<string> InternalNodes = AsNodes.Concat(OtherInternalNodes);
}
