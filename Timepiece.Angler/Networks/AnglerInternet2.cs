using System.Numerics;
using Timepiece.Angler.DataTypes;
using Timepiece.DataTypes;
using Timepiece.Networks;
using ZenLib;

namespace Timepiece.Angler.Networks;

using RouteEnvironmentTransfer = Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>;

/// <summary>
/// Internet2 benchmarks.
/// </summary>
public class AnglerInternet2 : AnnotatedNetwork<RouteEnvironment, string>
{
  /// <summary>
  /// Construct an AnglerInternet2 instance of an <c>AnnotatedNetwork</c>.
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="transferFunctions"></param>
  /// <param name="mergeFunction"></param>
  /// <param name="initialValues"></param>
  /// <param name="annotations"></param>
  /// <param name="modularProperties"></param>
  /// <param name="monolithicProperties"></param>
  /// <param name="symbolics"></param>
  public AnglerInternet2(Digraph<string> digraph,
    Dictionary<(string, string), RouteEnvironmentTransfer> transferFunctions,
    Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>, Zen<RouteEnvironment>> mergeFunction,
    Dictionary<string, Zen<RouteEnvironment>> initialValues,
    Dictionary<string, Func<Zen<RouteEnvironment>, Zen<BigInteger>, Zen<bool>>> annotations,
    Dictionary<string, Func<Zen<RouteEnvironment>, Zen<BigInteger>, Zen<bool>>> modularProperties,
    Dictionary<string, Func<Zen<RouteEnvironment>, Zen<bool>>> monolithicProperties, ISymbolic[] symbolics) : base(
    digraph, transferFunctions, mergeFunction, initialValues, annotations, modularProperties,
    monolithicProperties, symbolics)
  {
  }

  /// <summary>
  /// Construct an AnglerInternet2 instance that checks a safety property defined by <paramref name="monolithicProperties"/> on all nodes.
  /// The safety property must hold at all times, and is used directly as the annotation.
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="transferFunctions"></param>
  /// <param name="initialValues"></param>
  /// <param name="monolithicProperties"></param>
  /// <param name="symbolics"></param>
  public AnglerInternet2(Digraph<string> digraph,
    Dictionary<(string, string), Func<Zen<RouteEnvironment>, Zen<RouteEnvironment>>> transferFunctions,
    Dictionary<string, Zen<RouteEnvironment>> initialValues,
    Dictionary<string, Func<Zen<RouteEnvironment>, Zen<bool>>> monolithicProperties, ISymbolic[] symbolics) : this(
    digraph, transferFunctions, RouteEnvironmentExtensions.MinOptional, initialValues,
    new Dictionary<string, Func<Zen<RouteEnvironment>, Zen<BigInteger>, Zen<bool>>>(),
    new Dictionary<string, Func<Zen<RouteEnvironment>, Zen<BigInteger>, Zen<bool>>>(), monolithicProperties, symbolics)
  {
    ModularProperties = digraph.MapNodes(n => Lang.Globally(monolithicProperties[n]));
    Annotations = ModularProperties;
  }


  /// <summary>
  /// Lift the given AnglerInternet2 instance to one where links may arbitrarily fail.
  /// </summary>
  /// <param name="net"></param>
  /// <returns></returns>
  public static AnglerInternet2 FaultTolerance(AnglerInternet2 net)
  {
    // construct a dictionary of edge failure booleans
    var edgeFailed = SymbolicValue.SymbolicDictionary<(string, string), bool>("failed", net.Digraph.Edges());
    // if the failure boolean is true, the route is dropped
    var liftedTransferFunctions =
      net.Digraph.MapEdges<RouteEnvironmentTransfer>(e =>
        r => Zen.If(edgeFailed[e].Value, new RouteEnvironment(), net.TransferFunctions[e](r)));
    var symbolics = net.Symbolics.Concat(edgeFailed.Values).ToArray();
    return new AnglerInternet2(net.Digraph, liftedTransferFunctions, net.MergeFunction, net.InitialValues,
      net.Annotations, net.ModularProperties, net.MonolithicProperties, symbolics);
  }

  /// <summary>
  ///   The block to external community tag used by Internet2.
  /// </summary>
  private const string BlockToExternalCommunity = "11537:888";

  /// <summary>
  ///   Community tag for identifying low-value peer connections.
  /// </summary>
  private const string LowPeersCommunity = "11537:40";

  /// <summary>
  ///   Community tag for identifying lower-than-peer connections.
  /// </summary>
  private const string LowerThanPeersCommunity = "11537:60";

  /// <summary>
  ///   Community tag for identifying equal-to-peer-value connections.
  /// </summary>
  private const string EqualToPeersCommunity = "11537:100";

  /// <summary>
  ///   Community tag for identifying low-value connections.
  /// </summary>
  private const string LowCommunity = "11537:140";

  /// <summary>
  ///   Community tag for identifying high-value peer connections.
  /// </summary>
  private const string HighPeersCommunity = "11537:160";

  /// <summary>
  ///   Community tag for identifying high-value connections.
  /// </summary>
  private const string HighCommunity = "11537:260";

  /// <summary>
  /// Regular expression representing private AS numbers.
  /// </summary>
  public const string PrivateAs =
    @"^((^| )\d+)*(^| )(64512|64513|64514|64515|64516|64517|64518|64519|64520|64521|64522|64523|64524|64525|64526|64527|64528|64529|64530|64531|64532|64533|64534|64535|64536|64537|64538|64539|64540|64541|64542|64543|64544|64545|64546|64547|64548|64549|64550|64551|64552|64553|64554|64555|64556|64557|64558|64559|64560|64561|64562|64563|64564|64565|64566|64567|64568|64569|64570|64571|64572|64573|64574|64575|64576|64577|64578|64579|64580|64581|64582|64583|64584|64585|64586|64587|64588|64589|64590|64591|64592|64593|64594|64595|64596|64597|64598|64599|64600|64601|64602|64603|64604|64605|64606|64607|64608|64609|64610|64611|64612|64613|64614|64615|64616|64617|64618|64619|64620|64621|64622|64623|64624|64625|64626|64627|64628|64629|64630|64631|64632|64633|64634|64635|64636|64637|64638|64639|64640|64641|64642|64643|64644|64645|64646|64647|64648|64649|64650|64651|64652|64653|64654|64655|64656|64657|64658|64659|64660|64661|64662|64663|64664|64665|64666|64667|64668|64669|64670|64671|64672|64673|64674|64675|64676|64677|64678|64679|64680|64681|64682|64683|64684|64685|64686|64687|64688|64689|64690|64691|64692|64693|64694|64695|64696|64697|64698|64699|64700|64701|64702|64703|64704|64705|64706|64707|64708|64709|64710|64711|64712|64713|64714|64715|64716|64717|64718|64719|64720|64721|64722|64723|64724|64725|64726|64727|64728|64729|64730|64731|64732|64733|64734|64735|64736|64737|64738|64739|64740|64741|64742|64743|64744|64745|64746|64747|64748|64749|64750|64751|64752|64753|64754|64755|64756|64757|64758|64759|64760|64761|64762|64763|64764|64765|64766|64767|64768|64769|64770|64771|64772|64773|64774|64775|64776|64777|64778|64779|64780|64781|64782|64783|64784|64785|64786|64787|64788|64789|64790|64791|64792|64793|64794|64795|64796|64797|64798|64799|64800|64801|64802|64803|64804|64805|64806|64807|64808|64809|64810|64811|64812|64813|64814|64815|64816|64817|64818|64819|64820|64821|64822|64823|64824|64825|64826|64827|64828|64829|64830|64831|64832|64833|64834|64835|64836|64837|64838|64839|64840|64841|64842|64843|64844|64845|64846|64847|64848|64849|64850|64851|64852|64853|64854|64855|64856|64857|64858|64859|64860|64861|64862|64863|64864|64865|64866|64867|64868|64869|64870|64871|64872|64873|64874|64875|64876|64877|64878|64879|64880|64881|64882|64883|64884|64885|64886|64887|64888|64889|64890|64891|64892|64893|64894|64895|64896|64897|64898|64899|64900|64901|64902|64903|64904|64905|64906|64907|64908|64909|64910|64911|64912|64913|64914|64915|64916|64917|64918|64919|64920|64921|64922|64923|64924|64925|64926|64927|64928|64929|64930|64931|64932|64933|64934|64935|64936|64937|64938|64939|64940|64941|64942|64943|64944|64945|64946|64947|64948|64949|64950|64951|64952|64953|64954|64955|64956|64957|64958|64959|64960|64961|64962|64963|64964|64965|64966|64967|64968|64969|64970|64971|64972|64973|64974|64975|64976|64977|64978|64979|64980|64981|64982|64983|64984|64985|64986|64987|64988|64989|64990|64991|64992|64993|64994|64995|64996|64997|64998|64999|65000|65001|65002|65003|65004|65005|65006|65007|65008|65009|65010|65011|65012|65013|65014|65015|65016|65017|65018|65019|65020|65021|65022|65023|65024|65025|65026|65027|65028|65029|65030|65031|65032|65033|65034|65035|65036|65037|65038|65039|65040|65041|65042|65043|65044|65045|65046|65047|65048|65049|65050|65051|65052|65053|65054|65055|65056|65057|65058|65059|65060|65061|65062|65063|65064|65065|65066|65067|65068|65069|65070|65071|65072|65073|65074|65075|65076|65077|65078|65079|65080|65081|65082|65083|65084|65085|65086|65087|65088|65089|65090|65091|65092|65093|65094|65095|65096|65097|65098|65099|65100|65101|65102|65103|65104|65105|65106|65107|65108|65109|65110|65111|65112|65113|65114|65115|65116|65117|65118|65119|65120|65121|65122|65123|65124|65125|65126|65127|65128|65129|65130|65131|65132|65133|65134|65135|65136|65137|65138|65139|65140|65141|65142|65143|65144|65145|65146|65147|65148|65149|65150|65151|65152|65153|65154|65155|65156|65157|65158|65159|65160|65161|65162|65163|65164|65165|65166|65167|65168|65169|65170|65171|65172|65173|65174|65175|65176|65177|65178|65179|65180|65181|65182|65183|65184|65185|65186|65187|65188|65189|65190|65191|65192|65193|65194|65195|65196|65197|65198|65199|65200|65201|65202|65203|65204|65205|65206|65207|65208|65209|65210|65211|65212|65213|65214|65215|65216|65217|65218|65219|65220|65221|65222|65223|65224|65225|65226|65227|65228|65229|65230|65231|65232|65233|65234|65235|65236|65237|65238|65239|65240|65241|65242|65243|65244|65245|65246|65247|65248|65249|65250|65251|65252|65253|65254|65255|65256|65257|65258|65259|65260|65261|65262|65263|65264|65265|65266|65267|65268|65269|65270|65271|65272|65273|65274|65275|65276|65277|65278|65279|65280|65281|65282|65283|65284|65285|65286|65287|65288|65289|65290|65291|65292|65293|65294|65295|65296|65297|65298|65299|65300|65301|65302|65303|65304|65305|65306|65307|65308|65309|65310|65311|65312|65313|65314|65315|65316|65317|65318|65319|65320|65321|65322|65323|65324|65325|65326|65327|65328|65329|65330|65331|65332|65333|65334|65335|65336|65337|65338|65339|65340|65341|65342|65343|65344|65345|65346|65347|65348|65349|65350|65351|65352|65353|65354|65355|65356|65357|65358|65359|65360|65361|65362|65363|65364|65365|65366|65367|65368|65369|65370|65371|65372|65373|65374|65375|65376|65377|65378|65379|65380|65381|65382|65383|65384|65385|65386|65387|65388|65389|65390|65391|65392|65393|65394|65395|65396|65397|65398|65399|65400|65401|65402|65403|65404|65405|65406|65407|65408|65409|65410|65411|65412|65413|65414|65415|65416|65417|65418|65419|65420|65421|65422|65423|65424|65425|65426|65427|65428|65429|65430|65431|65432|65433|65434|65435|65436|65437|65438|65439|65440|65441|65442|65443|65444|65445|65446|65447|65448|65449|65450|65451|65452|65453|65454|65455|65456|65457|65458|65459|65460|65461|65462|65463|65464|65465|65466|65467|65468|65469|65470|65471|65472|65473|65474|65475|65476|65477|65478|65479|65480|65481|65482|65483|65484|65485|65486|65487|65488|65489|65490|65491|65492|65493|65494|65495|65496|65497|65498|65499|65500|65501|65502|65503|65504|65505|65506|65507|65508|65509|65510|65511|65512|65513|65514|65515|65516|65517|65518|65519|65520|65521|65522|65523|65524|65525|65526|65527|65528|65529|65530|65531|65532|65533|65534|65535)((^| )\d+)*$";

  /// <summary>
  /// Regular expression representing commercial AS peers.
  /// </summary>
  public const string CommercialAs =
    @"^((^| )\d+)*((^| )174|(^| )701|(^| )1239|(^| )1673|(^| )1740|(^| )1800|(^| )1833|(^| )2551|(^| )2548|(^| )2685|(^| )2914|(^| )3549|(^| )3561|(^| )3847|(^| )3951|(^| )3967|(^| )4183|(^| )4200|(^| )5683|(^| )6113|(^| )6172|(^| )6461|(^| )7018)((^| )\d+)*$";

  /// <summary>
  /// Regular expression representing NLR AS peers.
  /// </summary>
  public const string NlrAs = @"^((^| )\d+)*(^| )19401((^| )\d+)*$";

  /// <summary>
  ///   A prefix corresponding to the internal nodes of Internet2.
  /// </summary>
  private static readonly Ipv4Prefix InternalPrefix = new("64.57.28.0", "64.57.28.255");

  /// <summary>
  ///   Prefixes that are considered Martians.
  ///   Must not be advertised or accepted.
  ///   Mostly taken from Internet2's configs: see the SANITY-IN policy's block-martians term.
  /// </summary>
  public static readonly (Ipv4Prefix Prefix, bool Exact)[] MartianPrefixes =
  {
    (new Ipv4Prefix("0.0.0.0/0"), Exact: true), // default route 0.0.0.0/0
    (new Ipv4Prefix("10.0.0.0/8"), Exact: false), // RFC1918 local network
    (new Ipv4Prefix("127.0.0.0/8"), Exact: false), // RFC3330 loopback
    (new Ipv4Prefix("169.254.0.0/16"), Exact: false), // RFC330 link-local addresses
    (new Ipv4Prefix("172.16.0.0/12"), Exact: false), // RFC1918 private addresses
    (new Ipv4Prefix("192.0.2.0/24"), Exact: false), // IANA reserved
    (new Ipv4Prefix("192.88.99.1/32"), Exact: true), // 6to4 relay
    (new Ipv4Prefix("192.168.0.0/16"), Exact: false), // RFC1918 private addresses
    (new Ipv4Prefix("198.18.0.0/15"), Exact: false), // RFC2544 network device benchmarking
    (new Ipv4Prefix("224.0.0.0/4"), Exact: false), // RFC3171 multicast group addresses
    (new Ipv4Prefix("240.0.0.0/4"), Exact: false), // RFC3330 special-use addresses
    (new Ipv4Prefix("255.255.255.255/32"), Exact: true) // limited broadcast -- used?
  };

  /// <summary>
  /// List of prefixes which Abilene originates
  /// </summary>
  private static readonly (Ipv4Prefix Prefix, bool Exact)[] InternalPrefixes =
  {
    // Internet2 Backbone
    (new Ipv4Prefix("64.57.16.0/20"), Exact: false),
    // Transit VRF
    (new Ipv4Prefix("64.57.22.0/24"), Exact: false),
    (new Ipv4Prefix("64.57.23.240/28"), Exact: false),
    // Abilene Backbone
    (new Ipv4Prefix("198.32.8.0/22"), Exact: false),
    // Abilene Observatory
    (new Ipv4Prefix("198.32.12.0/22"), Exact: false),
    // MANLAN
    (new Ipv4Prefix("198.32.154.0/24"), Exact: false),
    (new Ipv4Prefix("198.71.45.0/24"), Exact: false),
    (new Ipv4Prefix("198.71.46.0/24"), Exact: false)
  };

  /// <summary>
  /// Shorthand predicate that a route's prefix length is valid.
  /// </summary>
  /// <param name="env"></param>
  /// <returns></returns>
  private static Zen<bool> HasValidPrefixLength(Zen<RouteEnvironment> env) => env.GetPrefix().IsValidPrefixLength();

  /// <summary>
  ///   Return true if none of the given Ipv4 wildcards match the given prefix
  /// </summary>
  /// <param name="candidates"></param>
  /// <param name="prefix"></param>
  /// <returns></returns>
  public static Zen<bool> NoPrefixMatch(IEnumerable<(Ipv4Prefix Prefix, bool Exact)> candidates,
    Zen<Ipv4Prefix> prefix)
  {
    return candidates.ForAll(candidate =>
      Zen.Not(candidate.Prefix.Matches(prefix, candidate.Exact)));
  }

  /// <summary>
  ///   Construct an AnnotatedNetwork with constraints that every external node symbolic does not have the BTE tag,
  ///   and check that all external nodes never have a BTE tag.
  /// </summary>
  /// <param name="digraph">A network topology.</param>
  /// <param name="externalPeers">An enumerable of external peer nodes.</param>
  /// <param name="transferFunctions">The transfer functions of the network.</param>
  /// <returns>An <c>AnglerInternet2</c> instance.</returns>
  public static AnglerInternet2 BlockToExternal(Digraph<string> digraph,
    IEnumerable<string> externalPeers,
    Dictionary<(string, string), RouteEnvironmentTransfer> transferFunctions)
  {
    var externalRoutes =
      SymbolicValue.SymbolicDictionary<string, RouteEnvironment>("external-route", externalPeers, BteTagAbsent);
    // external nodes start with a route, internal nodes do not
    var initialRoutes = digraph.MapNodes(n =>
      externalRoutes.TryGetValue(n, out var route) ? route.Value : new RouteEnvironment());

    var monolithicProperties =
      digraph.MapNodes<Func<Zen<RouteEnvironment>, Zen<bool>>>(n =>
        Internet2Nodes.InternalNodes.Contains(n)
        || n == "10.11.1.17"
        || Internet2Nodes.OtherGroup.Contains(n)
          ? Lang.True<RouteEnvironment>() : BteTagAbsent);
    var symbolics = externalRoutes.Values.Cast<ISymbolic>().ToArray();

    return new AnglerInternet2(digraph, transferFunctions, initialRoutes, monolithicProperties, symbolics);
  }

  /// <summary>
  ///   Predicate that the BTE tag is not on the route if the route has a value.
  /// </summary>
  private static Zen<bool> BteTagAbsent(Zen<RouteEnvironment> env)
  {
    return Zen.Implies(env.GetResultValue(),
      Zen.Not(env.GetCommunities().Contains(BlockToExternalCommunity)));
  }

  /// <summary>
  /// Verify that the internal nodes never select a route for a Martian prefix.
  /// </summary>
  /// <param name="digraph">A network topology.</param>
  /// <param name="externalPeers">An enumerable of external peer nodes.</param>
  /// <param name="transferFunctions">The transfer functions of the network.</param>
  /// <returns>An <c>AnglerInternet2</c> instance.</returns>
  public static AnglerInternet2 NoMartians(Digraph<string> digraph, IEnumerable<string> externalPeers,
    Dictionary<(string, string), RouteEnvironmentTransfer> transferFunctions)
  {
    var externalPrefix = new SymbolicValue<Ipv4Prefix>("external-prefix", p => p.IsValidPrefixLength());
    // all external routes must be for the external prefix
    var externalRoutes =
      SymbolicValue.SymbolicDictionary<string, RouteEnvironment>("external-route", externalPeers,
        r => r.GetPrefix() == externalPrefix.Value);
    var initialRoutes = digraph.MapNodes(n =>
      externalRoutes.TryGetValue(n, out var route) ? route.Value : new RouteEnvironment());

    // internal nodes must not select martian routes
    var monolithicProperties = digraph.MapNodes(n =>
      Internet2Nodes.InternalNodes.Contains(n) ||
      n == "10.11.1.17" // special case: "tata-Telepresence peering| I2-S13170", which does not use a SANITY-IN policy
        ? Lang.Intersect<RouteEnvironment>(
          // route is non-martian
          env => Zen.Implies(env.GetResultValue(),
            NoPrefixMatch(MartianPrefixes, env.GetPrefix())),
          // route has prefix length at most 32
          HasValidPrefixLength)
        // external nodes can have any route
        : Lang.True<RouteEnvironment>());
    var symbolics = externalRoutes.Values.Cast<ISymbolic>().Append(externalPrefix).ToArray();
    var modularProperties = digraph.MapNodes(n => Lang.Globally(monolithicProperties[n]));
    return new AnglerInternet2(digraph, transferFunctions,
      (r1, r2) => RouteEnvironmentExtensions.MinOptionalForPrefix(r1, r2, externalPrefix.Value), initialRoutes,
      modularProperties, modularProperties, monolithicProperties, symbolics);
  }

  /// <summary>
  /// Verify that the internal nodes never select a Martian prefix.
  /// </summary>
  /// <param name="digraph">A network topology.</param>
  /// <param name="externalPeers">An enumerable of external peer nodes.</param>
  /// <param name="transferFunctions">The transfer functions of the network.</param>
  /// <returns>An <c>AnglerInternet2</c> instance.</returns>
  /// <remarks>
  /// Unlike <see cref="NoMartians"/>, here we assume that the external nodes (may) have a Martian prefix,
  /// and want to show that the internal nodes never select a route.
  /// </remarks>
  public static AnglerInternet2 NoMartiansContrapositive(Digraph<string> digraph, IEnumerable<string> externalPeers,
    Dictionary<(string, string), RouteEnvironmentTransfer> transferFunctions)
  {
    // the external prefix is Martian
    var externalPrefix = new SymbolicValue<Ipv4Prefix>("external-prefix",
      p => MartianPrefixes.Exists(martian => martian.Prefix.Matches(p, martian.Exact)));
    // all external routes must be for the external prefix
    var externalRoutes =
      SymbolicValue.SymbolicDictionary<string, RouteEnvironment>("external-route", externalPeers,
        r => r.GetPrefix() == externalPrefix.Value);
    // special case: "tata-Telepresence peering| I2-S13170", which does not use a SANITY-IN policy
    // the configs state however that it should "never send us Martian routes"
    externalRoutes["10.11.1.17"].Constraint =
      r => Zen.And(Zen.Not(r.GetResultValue()), r.GetPrefix() == externalPrefix.Value);
    var initialRoutes = digraph.MapNodes(n =>
      externalRoutes.TryGetValue(n, out var route) ? route.Value : new RouteEnvironment());

    // internal nodes must never have a route
    var monolithicProperties = digraph.MapNodes(n =>
      Internet2Nodes.InternalNodes.Contains(n)
      ||
      n == "10.11.1.17" // special case: "tata-Telepresence peering| I2-S13170", which does not use a SANITY-IN policy
        ? env => Zen.Not(env.GetResultValue())
        // external nodes can have any route
        : Lang.True<RouteEnvironment>());
    var symbolics = externalRoutes.Values.Cast<ISymbolic>().Append(externalPrefix).ToArray();
    var modularProperties = digraph.MapNodes(n => Lang.Globally(monolithicProperties[n]));
    return new AnglerInternet2(digraph, transferFunctions,
      (r1, r2) => RouteEnvironmentExtensions.MinOptionalForPrefix(r1, r2, externalPrefix.Value), initialRoutes,
      modularProperties, modularProperties, monolithicProperties, symbolics);
  }

  /// <summary>
  /// Verify that the internal nodes never select a route with a private AS in the path.
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="externalPeers"></param>
  /// <param name="transferFunctions"></param>
  /// <returns></returns>
  public static AnglerInternet2 NoPrivateAs(Digraph<string> digraph,
    IEnumerable<string> externalPeers,
    Dictionary<(string, string), RouteEnvironmentTransfer> transferFunctions)
  {
    var externalRoutes =
      SymbolicValue.SymbolicDictionary<string, RouteEnvironment>("external-route", externalPeers,
        HasValidPrefixLength);
    var initialRoutes = digraph.MapNodes(n =>
      externalRoutes.TryGetValue(n, out var route) ? route.Value : new RouteEnvironment());

    // internal nodes must not select private AS routes
    var monolithicProperties = digraph.MapNodes(n => Internet2Nodes.InternalNodes.Contains(n)
      ? Lang.Intersect<RouteEnvironment>(
        env => Zen.Implies(env.GetResultValue(), Zen.Not(env.GetAsSet().Contains(PrivateAs))),
        HasValidPrefixLength)
      : Lang.True<RouteEnvironment>());
    var symbolics = externalRoutes.Values.Cast<ISymbolic>().ToArray();
    return new AnglerInternet2(digraph, transferFunctions, initialRoutes, monolithicProperties,
      symbolics);
  }

  /// <summary>
  /// Verify that the internal nodes never select a route with a private AS in the path.
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="externalPeers"></param>
  /// <param name="transferFunctions"></param>
  /// <returns></returns>
  /// <remarks>
  /// Unlike <see cref="NoPrivateAs"/>, here we assume that the external nodes (may) have a route with a private AS,
  /// and want to show that the internal nodes never select a route.
  /// </remarks>
  public static AnglerInternet2 NoPrivateAsContrapositive(Digraph<string> digraph, IEnumerable<string> externalPeers,
    Dictionary<(string, string), RouteEnvironmentTransfer> transferFunctions)
  {
    var externalRoutes =
      SymbolicValue.SymbolicDictionary("external-route", externalPeers,
        Lang.Intersect<RouteEnvironment>(HasValidPrefixLength, r => r.GetAsSet().Contains(PrivateAs)));
    var initialRoutes = digraph.MapNodes(n =>
      externalRoutes.TryGetValue(n, out var route) ? route.Value : new RouteEnvironment());

    // external nodes must not select routes for private AS routes
    var monolithicProperties = digraph.MapNodes(n => Internet2Nodes.InternalNodes.Contains(n)
      ? env => Zen.Not(env.GetResultValue())
      : Lang.True<RouteEnvironment>());
    var symbolics = externalRoutes.Values.Cast<ISymbolic>().ToArray();
    return new AnglerInternet2(digraph, transferFunctions, initialRoutes, monolithicProperties, symbolics);
  }

  private static Zen<bool> HasInternalPrefixRoute(Zen<RouteEnvironment> r) =>
    Zen.And(r.GetResultValue(), r.GetPrefix() == InternalPrefix);

  /// <summary>
  /// Verify that all the internal nodes receive a valid route if one is shared by one of them to the others.
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="transferFunctions"></param>
  /// <returns></returns>
  public static AnglerInternet2 ReachableInternal(Digraph<string> digraph,
    Dictionary<(string, string), RouteEnvironmentTransfer> transferFunctions)
  {
    var internalRoutes = SymbolicValue.SymbolicDictionary<string, RouteEnvironment>("internal-route",
      Internet2Nodes.AsNodes, HasInternalPrefixRoute);
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(2);
    var initialRoutes = digraph.MapNodes(n =>
      internalRoutes.TryGetValue(n, out var internalRoute)
        ? internalRoute.Value
        : new RouteEnvironment {Prefix = InternalPrefix});
    var monolithicProperties = digraph.MapNodes(n => Internet2Nodes.AsNodes.Contains(n)
      // internal nodes have a route if one of them has one initially
      ? r => Zen.Implies(
        // if one of the internal routes is true,
        internalRoutes.Values.Exists(ir => ir.Value.GetResultValue()),
        // then all the internal nodes will have routes
        HasInternalPrefixRoute(r))
      // no check on external nodes
      : Lang.True<RouteEnvironment>());
    var modularProperties = digraph.MapNodes(n => Internet2Nodes.AsNodes.Contains(n)
      ? Lang.Finally(
        // if the node starts with a route, then it gets one at time 0, otherwise at time 1
        Zen.If(internalRoutes[n].Value.GetResultValue(), symbolicTimes[0].Value, symbolicTimes[1].Value),
        monolithicProperties[n])
      : Lang.Globally(monolithicProperties[n]));
    var annotations = digraph.MapNodes(n =>
      Lang.Intersect(modularProperties[n],
        Lang.Globally(RouteEnvironment.IfValue(r => r.GetPrefix() == InternalPrefix))));
    var symbolics = internalRoutes.Values.Cast<ISymbolic>().Concat(symbolicTimes).ToArray();
    return new AnglerInternet2(digraph, transferFunctions,
      (r1, r2) => RouteEnvironmentExtensions.MinOptionalForPrefix(r1, r2, InternalPrefix), initialRoutes, annotations,
      modularProperties,
      monolithicProperties, symbolics);
  }


  /// <summary>
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="destination"></param>
  /// <param name="externalPeers"></param>
  /// <param name="transferFunctions"></param>
  /// <returns></returns>
  public static AnglerInternet2 Reachable(Digraph<string> digraph, string destination,
    IEnumerable<string> externalPeers,
    Dictionary<(string, string), RouteEnvironmentTransfer> transferFunctions)
  {
    if (!Internet2Prefixes.ExternalPeerParticipantList.TryGetValue(destination, out var participantList))
    {
      throw new ArgumentOutOfRangeException(nameof(destination), destination,
        "Destination must be an external peer in the participant list.");
    }

    // the announced external destination prefix
    var destinationPrefix = new SymbolicValue<Ipv4Prefix>("external-prefix", p =>
      // must be for this destination node's prefix and no smaller than /27
      Internet2Prefixes.ParticipantPrefixes[participantList.Participant].Exists(prefix =>
        Zen.And(prefix.Matches(p, participantList.Exact),
          // if the participant list is not exact, restrict the prefix length to at most /27
          // (in order for the CONNECTOR-IN to accept it)
          participantList.Exact ? true : p.GetPrefixLength() <= new UInt<_6>(27))));
    var externalRoutes = externalPeers.ToDictionary(e => e,
      e => new SymbolicValue<RouteEnvironment>($"external-route-{e}",
        // external route is for the destination prefix, and has a value if the external peer is the destination
        r => Zen.And(r.GetPrefix() == destinationPrefix.Value, Zen.Implies(e == destination, r.GetResultValue()))));
    var initialRoutes = digraph.MapNodes(n =>
      externalRoutes.TryGetValue(n, out var route)
        ? route.Value
        : Zen.Constant(new RouteEnvironment()).WithPrefix(destinationPrefix.Value));
    // there are 2 symbolic times: when the internal nodes adjacent to the external peer get a route, and when the other internal nodes get a route
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(2);
    // make the external adjacent time constraint strictly greater than 0
    symbolicTimes[0].Constraint = t => t > BigInteger.Zero;
    var nextToPeerTime = symbolicTimes[0].Value;
    var notNextToPeerTime = symbolicTimes[1].Value;
    var lastTime = symbolicTimes[^1].Value;

    var monolithicProperties = digraph.MapNodes(n => Internet2Nodes.AsNodes.Contains(n)
      // Internet2 nodes: if an external route exists, then we must have a route
      ? r => r.HasPrefixRoute(destinationPrefix.Value)
      // no check on external nodes
      : Lang.True<RouteEnvironment>());
    var modularProperties = digraph.MapNodes(n => Internet2Nodes.AsNodes.Contains(n)
      // eventually, if an external route exists, internal nodes have a route
      ? Lang.Finally(lastTime, monolithicProperties[n])
      : Lang.Globally(monolithicProperties[n]));
    var annotations = digraph.MapNodes(n =>
    {
      // internal nodes get routes at 2 different possible times
      // case 1: an adjacent external peer has a route. we get a route once they send it to us
      // case 2: no adjacent external peer has a route. we get a route once an internal neighbor sends it to us
      var witnessTime = Zen.If(ExternalNeighborHasRoute(digraph, n, externalRoutes), nextToPeerTime, notNextToPeerTime);
      // safety condition: if a node has a route, then the route must be for the destination prefix
      // and not include any bad AS number
      var safety = Lang.Globally<RouteEnvironment>(r =>
        Zen.Implies(r.GetResultValue(), Zen.And(r.GetPrefix() == destinationPrefix.Value, NoBadAs(r))));
      return Lang.Intersect(Internet2Nodes.AsNodes.Contains(n)
          ? Lang.Finally(witnessTime, monolithicProperties[n])
          // if an external node starts with a route, it must always have one (for its specific starting prefix)
          // otherwise, we don't care what route it has
          : Lang.Globally<RouteEnvironment>(r =>
            externalRoutes.TryGetValue(n, out var externalRoute)
              ? Zen.Implies(externalRoute.Value.GetResultValue(), r.HasPrefixRoute(externalRoute.Value.GetPrefix()))
              : Zen.True()),
        safety);
    });
    var symbolics = externalRoutes.Values.Cast<ISymbolic>().Concat(symbolicTimes).Append(destinationPrefix).ToArray();
    return new AnglerInternet2(digraph, transferFunctions,
      (r1, r2) => RouteEnvironmentExtensions.MinOptionalForPrefix(r1, r2, destinationPrefix.Value),
      initialRoutes, annotations, modularProperties, monolithicProperties, symbolics);
  }

  /// <summary>
  /// Verify that if a valid route comes from the external peers to the network,
  /// then all the internal nodes eventually have that route.
  /// The route has a symbolic destination prefix, which must match a whitelisted
  /// prefix of one of the network's *-PARTICIPANT prefix lists.
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="externalPeers"></param>
  /// <param name="transferFunctions"></param>
  /// <returns></returns>
  public static AnglerInternet2 ReachableSymbolicPrefix(Digraph<string> digraph,
    Dictionary<string, IEnumerable<(Ipv4Prefix Prefix, bool Exact)>> externalPeers,
    Dictionary<(string, string), RouteEnvironmentTransfer> transferFunctions)
  {
    // the announced external destination prefix
    var destinationPrefix = new SymbolicValue<Ipv4Prefix>("external-prefix", p =>
      Zen.And(
        // (1) must not be for a martian prefix or an Internet2-internal prefix
        NoPrefixMatch(MartianPrefixes.Concat(InternalPrefixes), p),
        // (2) must have a prefix length of at most /27 -- higher lengths will be dropped by CONNECTOR-IN
        p.GetPrefixLength() <= new UInt<_6>(27)));
    // two cases for external routes: if they have a prefix that matches the destination prefix:
    // a. if they have a route, the prefix must match the destination prefix and the route must not have any bad ASes; or
    // b. there is no route
    var externalRoutes = externalPeers.ToDictionary(e => e.Key,
      e =>
      {
        var matchesPrefix =
          e.Value.Exists(prefix => prefix.Prefix.Matches(destinationPrefix.Value, exact: prefix.Exact));
        // the constraint says that if the prefix matches, then there should be a route for it (if one exists);
        // if the prefix does not match, this neighbor should not send a route
        return new SymbolicValue<RouteEnvironment>($"external-route-{e}",
          r => Zen.If(matchesPrefix,
            Zen.Implies(r.GetResultValue(), Zen.And(r.GetPrefix() == destinationPrefix.Value, NoBadAs(r))),
            Zen.Not(r.GetResultValue())));
      });
    var initialRoutes = digraph.MapNodes(n =>
      externalRoutes.TryGetValue(n, out var route)
        ? route.Value
        : Zen.Constant(new RouteEnvironment()).WithPrefix(destinationPrefix.Value));
    var symbolicTimes = SymbolicTime.AscendingSymbolicTimes(3); // there are 3 symbolic times:
    // when the external peers have a route;
    var externalTime = symbolicTimes[0].Value;
    // when the internal nodes adjacent to the external peer get a route;
    var nextToPeerTime = symbolicTimes[1].Value;
    // when the other internal nodes get a route
    var notNextToPeerTime = symbolicTimes[2].Value;
    var lastTime = symbolicTimes[^1].Value;
    // encoding that an external route exists
    var externalRouteExists = externalRoutes
      // external route exists at a non-AL2S_MGMT/OTHER/OTHER_INTERNAL neighbor
      .Where(ext =>
        !Internet2Nodes.AdvancedLayer2ServiceManagementGroup.Concat(Internet2Nodes.OtherGroup)
          .Concat(Internet2Nodes.OtherInternalGroup).Contains(ext.Key))
      .Select(ext => ext.Value.Value)
      .Exists(r => r.HasPrefixRoute(destinationPrefix.Value));

    var monolithicProperties = digraph.MapNodes(n => Internet2Nodes.AsNodes.Contains(n)
      // Internet2 nodes: if an external route exists, then we must have a route
      ? r => Zen.Implies(externalRouteExists, r.HasPrefixRoute(destinationPrefix.Value))
      // no check on external nodes
      : Lang.True<RouteEnvironment>());
    var modularProperties = digraph.MapNodes(n => Internet2Nodes.AsNodes.Contains(n)
      // eventually, if an external route exists, internal nodes have a route
      ? Lang.Finally(lastTime, monolithicProperties[n])
      : Lang.Globally(monolithicProperties[n]));
    var annotations = digraph.MapNodes(n =>
    {
      // internal nodes get routes at 2 different possible times
      // case 1: an adjacent external peer has a route. we get a route once they send it to us
      // case 2: no adjacent external peer has a route. we get a route once an internal neighbor sends it to us
      var witnessTime = Zen.If(ExternalNeighborHasRoute(digraph, n, externalRoutes), nextToPeerTime, notNextToPeerTime);
      // safety condition: if a node has a route, then the route must be for the destination prefix and not include a bad AS
      var safety = Lang.Globally<RouteEnvironment>(r =>
        Zen.Implies(r.GetResultValue(), Zen.And(r.GetPrefix() == destinationPrefix.Value, NoBadAs(r))));
      return Lang.Intersect(Internet2Nodes.AsNodes.Contains(n)
        ? Lang.Finally(witnessTime, monolithicProperties[n])
        // if an external node starts with a route, it must always have one (for its specific starting prefix)
        // otherwise, we don't care what route it has
        : Lang.Finally<RouteEnvironment>(externalTime, r =>
          externalRoutes.TryGetValue(n, out var externalRoute)
            ? Zen.Implies(externalRoute.Value.GetResultValue(), r.HasPrefixRoute(externalRoute.Value.GetPrefix()))
            : Zen.True()), safety);
    });
    var symbolics = externalRoutes.Values.Cast<ISymbolic>().Concat(symbolicTimes).Append(destinationPrefix).ToArray();
    return new AnglerInternet2(digraph, transferFunctions,
      (r1, r2) => RouteEnvironmentExtensions.MinOptionalForPrefix(r1, r2, destinationPrefix.Value), initialRoutes,
      annotations, modularProperties, monolithicProperties, symbolics);
  }

  /// <summary>
  /// Return a Zen value encoding that r's AS set contains none of the "bad ASes":
  /// <see cref="PrivateAs"/>, <see cref="NlrAs"/>, <see cref="CommercialAs"/>.
  /// </summary>
  /// <param name="r">A route.</param>
  /// <returns>A Zen boolean value.</returns>
  private static Zen<bool> NoBadAs(Zen<RouteEnvironment> r) =>
    new[] {PrivateAs, NlrAs, CommercialAs}.ForAll(asn => Zen.Not(r.GetAsSet().Contains(asn)));

  /// <summary>
  ///   Return a constraint that one of the node's external neighbors has a route.
  /// </summary>
  /// <param name="digraph"></param>
  /// <param name="node"></param>
  /// <param name="externalRoutes"></param>
  /// <returns></returns>
  private static Zen<bool> ExternalNeighborHasRoute(Digraph<string> digraph, string node,
    IReadOnlyDictionary<string, SymbolicValue<RouteEnvironment>> externalRoutes) =>
    digraph[node].Exists(neighbor =>
      // we use the ternary operator here ?: since we want this to short-circuit if externalRoutes does not contain the neighbor;
      // if we converted the first component to a Zen<bool>, we'd end up evaluating externalRoute.Value even if externalRoute is null
      externalRoutes.TryGetValue(neighbor, out var externalRoute)
        ? externalRoute.Value.GetResultValue()
        : Zen.False());
}
