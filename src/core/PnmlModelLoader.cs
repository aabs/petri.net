#nullable disable

using System.Diagnostics.Contracts;
using System.Xml.Linq;

namespace petrinets2.core;

[Obsolete("PnmlModelLoader is deprecated. Use PnmlStreamingModelLoader.LoadGraph(...) or LoadMatrix(...) for new code.")]
public static class PnmlModelLoader
{
    private static int seed;

    public static int GetId()
    {
        return ++seed;
    }

    public static IDictionary<string, Marking> LoadMarkings(string path, IEnumerable<GraphPetriNet> nets)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(nets);

        XNamespace ns = "http://www.example.org/pnml";
        XDocument doc = XDocument.Load(path);

        var netsById = nets.ToDictionary(net => net.Id, StringComparer.Ordinal);
        var placeIndexByNetId = nets.ToDictionary(
            net => net.Id,
            net => net.Places.ToDictionary(place => place.Value, place => place.Key, StringComparer.Ordinal),
            StringComparer.Ordinal);

        var netmarkings = from n in doc.Descendants(ns + "net")
                          let netid = n.Attribute("id").Value
                          let net = netsById[netid]
                          let placeLookup = placeIndexByNetId[netid]
                          let placeMarkings = from p in n.Descendants(ns + "place")
                                              let placeId = p.Attribute("id").Value
                                              let place = ResolvePlaceIndex(placeLookup, netid, placeId)
                                              let Marking =
                                                  int.Parse(
                                                      p.Element(ns + "initialMarking").Element(ns + "text").Value ??
                                                      "0")
                                              orderby place
                                              select Tuple.Create(place, Marking)
                          let maxId = placeMarkings.Max(x => x.Item1)
                          select new
                                     {
                                         name = netid,
                                         marking = new Marking(maxId + 1,
                                                               placeMarkings.ToDictionary(x => x.Item1, x => x.Item2))
                                     };
        return netmarkings.ToDictionary(x => x.name, x => x.marking);
    }

    public static IEnumerable<GraphPetriNet> Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        XNamespace ns = "http://www.example.org/pnml";
        XDocument doc = XDocument.Load(path);
        var result = new List<GraphPetriNet>();

        foreach (var netElement in doc.Descendants(ns + "net"))
        {
            var type = netElement.Attribute("type")?.Value;
            if (!string.Equals(type, "http://www.example.org/pnml/PTNet", StringComparison.Ordinal))
            {
                continue;
            }

            var netId = netElement.Attribute("id")?.Value ?? throw new ApplicationException("PNML net is missing id attribute.");
            var places = netElement.Descendants(ns + "place")
                .Select((place, index) => new
                {
                    Guid = index,
                    Id = place.Attribute("id")?.Value ?? throw new ApplicationException($"PNML place in net '{netId}' is missing id attribute.")
                })
                .ToList();

            var transitions = netElement.Descendants(ns + "transition")
                .Select((transition, index) => new
                {
                    Guid = index,
                    Id = transition.Attribute("id")?.Value ?? throw new ApplicationException($"PNML transition in net '{netId}' is missing id attribute.")
                })
                .ToList();

            var placeIdLookup = places.ToDictionary(place => place.Id, place => place.Guid, StringComparer.Ordinal);
            var transitionIdLookup = transitions.ToDictionary(transition => transition.Id, transition => transition.Guid, StringComparer.Ordinal);

            var inArcs = transitions.ToDictionary(
                transition => transition.Guid,
                transition => ResolveInArcs(netElement, transition.Id, placeIdLookup, netId, ns));

            var outArcs = transitions.ToDictionary(
                transition => transition.Guid,
                transition => ResolveOutArcs(netElement, transition.Id, placeIdLookup, netId, ns));

            result.Add(new GraphPetriNet(
                netId,
                places.ToDictionary(place => place.Guid, place => place.Id),
                transitions.ToDictionary(transition => transition.Guid, transition => transition.Id),
                inArcs,
                outArcs));
        }

        return result;
    }

    static int ResolvePlaceIndex(Dictionary<string, int> placeLookup, string netId, string placeId)
    {
        if (placeLookup.TryGetValue(placeId, out var index))
        {
            return index;
        }

        throw new KeyNotFoundException($"PNML net '{netId}' references place id '{placeId}', but no matching place was loaded.");
    }

    static List<InArc> ResolveInArcs(XElement netElement, string transitionId, Dictionary<string, int> placeIdLookup, string netId, XNamespace ns)
    {
        var arcs = new List<InArc>();
        foreach (var arc in netElement.Descendants(ns + "arc").Where(element => string.Equals(element.Attribute("target")?.Value, transitionId, StringComparison.Ordinal)))
        {
            var sourceId = arc.Attribute("source")?.Value ?? string.Empty;
            if (!placeIdLookup.TryGetValue(sourceId, out var sourceIndex))
            {
                var arcId = arc.Attribute("id")?.Value ?? "<unknown>";
                throw new KeyNotFoundException($"PNML net '{netId}' arc '{arcId}' references source place id '{sourceId}', but no matching place was loaded.");
            }

            arcs.Add(new InArc(sourceIndex));
        }

        return arcs;
    }

    static List<OutArc> ResolveOutArcs(XElement netElement, string transitionId, Dictionary<string, int> placeIdLookup, string netId, XNamespace ns)
    {
        var arcs = new List<OutArc>();
        foreach (var arc in netElement.Descendants(ns + "arc").Where(element => string.Equals(element.Attribute("source")?.Value, transitionId, StringComparison.Ordinal)))
        {
            var targetId = arc.Attribute("target")?.Value ?? string.Empty;
            if (!placeIdLookup.TryGetValue(targetId, out var targetIndex))
            {
                var arcId = arc.Attribute("id")?.Value ?? "<unknown>";
                throw new KeyNotFoundException($"PNML net '{netId}' arc '{arcId}' references target place id '{targetId}', but no matching place was loaded.");
            }

            arcs.Add(new OutArc(targetIndex, 1));
        }

        return arcs;
    }
}

public class NewPnmlLoader<TModelType> where TModelType : class
{
    public NewPnmlLoader()
    {
    }

    protected CreatePetriNet Builder { get; set; }

    public IEnumerable<TModelType> Load(string modelPath)
    {
        Contract.Requires(!string.IsNullOrWhiteSpace(modelPath));
        if (!File.Exists(modelPath))
        {
            throw new ApplicationException("Model cannot be found");
        }
        XDocument doc = XDocument.Parse(File.ReadAllText(modelPath));
        if (doc == null)
        {
            throw new ApplicationException("Unable to read contents of model file");
        }

        var names = GetModelNames(doc);

        foreach (string name in names)
        {
            var netroot = doc.Descendants("net").Where(element => element.Attribute("id").Value.Equals(name)).Single();
            this.Builder = new CreatePetriNet(name);
            var placeNames = GatherPlaceNames(netroot);
            var transitionNames = GatherTransitionNames(netroot);
            var inArcs = GatherInArcs(netroot, transitionNames);
            var outArcs = GatherOutArcs(netroot, transitionNames);
            Builder.WithPlaces(placeNames.Select(tuple => tuple.Item1).ToArray());
            Builder.WithTransitions(transitionNames.Select(tuple => tuple.Item1).ToArray());
            foreach (var inArc in inArcs)
            {
                var arc = Builder.With(inArc.Item3).FedBy(inArc.Item2).Weight(inArc.Item4);
                if (inArc.Item5) // is inhibitor
                {
                    arc.AsInhibitor();
                }
                arc.Done();
            }
            foreach (var outArc in outArcs)
            {
                var arc = Builder.With(outArc.Item2).Feeding(outArc.Item3).Weight(outArc.Item4);
                arc.Done();
            }
            yield return Builder.CreateNet<TModelType>();
        }
    }

    //(arcname, placename, tranname, weight, inhib)
    IEnumerable<Tuple<string, string, string, int, bool>> GatherInArcs(XElement doc, IEnumerable<Tuple<string, int>> transitions)
    {
        return from x in doc.Descendants("arc")
               let name = x.Attribute("id").Value
               let placeName = x.Attribute("source").Value
               let tranName = x.Attribute("target").Value
               let weight = x.Descendants("inscription").Descendants("value").SingleOrDefault()
               let inhibitor = x.Descendants("type").Where(a => a.Attribute("value").Value == "inhibitor").Count() == 1
               where transitions.Any(tuple => tuple.Item1.Equals(tranName))
               select Tuple.Create(
                    name, 
                    placeName, 
                    tranName,
                    weight != null ? int.Parse(weight.Value) : 1, 
                    inhibitor);
    }

    //(arcname, tranname, placename, weight)
    IEnumerable<Tuple<string, string, string, int>> GatherOutArcs(XElement doc, IEnumerable<Tuple<string, int>> transitions)
    {
        return from x in doc.Descendants("arc")
               let name = x.Attribute("id").Value
               let placeName = x.Attribute("target").Value
               let tranName = x.Attribute("source").Value
               let weight = x.Descendants("inscription").Descendants("value").SingleOrDefault()
               where transitions.Any(tuple => tuple.Item1.Equals(tranName))
               select Tuple.Create(
                    name,
                    tranName,
                    placeName,
                    weight != null ? int.Parse(weight.Value) : 1);
    }

    private IEnumerable<Tuple<string, int>> GatherTransitionNames(XElement doc)
    {
        return from e in doc.Descendants("transition")
               let priority = e.Descendants("priority").Descendants("value").SingleOrDefault()
               select Tuple.Create(e.Attribute("id").Value, int.Parse((priority != null) ? priority.Value.Trim() : "1"));
    }

    private IEnumerable<Tuple<string, int>> GatherPlaceNames(XElement doc)
    {
        return from e in doc.Descendants("place")
               let capacity = e.Descendants("capacity").Descendants("value").SingleOrDefault()
               select Tuple.Create(e.Attribute("id").Value, int.Parse((capacity != null) ? capacity.Value.Trim() : "0"));
    }

    private IEnumerable<string> GetModelNames(XDocument doc)
    {
        return from n in doc.Descendants("net")
               where n.Attribute("type").Value == "P/T net"
               select n.Attribute("id").Value;
    }
}