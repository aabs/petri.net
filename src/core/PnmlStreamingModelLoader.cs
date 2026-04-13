namespace petrinets2.core;

using System.Xml;

public enum PnmlTargetModel
{
    Graph,
    Matrix
}

public sealed class PnmlStreamingModelLoader
{
    static readonly XmlReaderSettings ReaderSettings = new()
    {
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
        IgnoreWhitespace = true,
        DtdProcessing = DtdProcessing.Prohibit
    };

    public IReadOnlyList<GraphPetriNet> LoadGraph(string path)
    {
        return Load(path, PnmlTargetModel.Graph).Cast<GraphPetriNet>().ToList();
    }

    public IReadOnlyList<MatrixPetriNet> LoadMatrix(string path)
    {
        return Load(path, PnmlTargetModel.Matrix).Cast<MatrixPetriNet>().ToList();
    }

    public IReadOnlyList<TNet> Load<TNet>(string path) where TNet : PetriNetBase
    {
        if (typeof(TNet) == typeof(GraphPetriNet))
        {
            return (IReadOnlyList<TNet>)LoadGraph(path);
        }

        if (typeof(TNet) == typeof(MatrixPetriNet))
        {
            return (IReadOnlyList<TNet>)LoadMatrix(path);
        }

        throw new NotSupportedException($"Unsupported net target type '{typeof(TNet).Name}'.");
    }

    public IReadOnlyList<PetriNetBase> Load(string path, PnmlTargetModel target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"PNML file '{path}' was not found.", path);
        }

        var results = new List<PetriNetBase>();

        using var reader = XmlReader.Create(path, ReaderSettings);
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || !reader.LocalName.Equals("net", StringComparison.Ordinal))
            {
                continue;
            }

            var type = reader.GetAttribute("type") ?? string.Empty;
            if (!IsSupportedNetType(type))
            {
                reader.Skip();
                continue;
            }

            var netId = reader.GetAttribute("id");
            if (string.IsNullOrWhiteSpace(netId))
            {
                throw new InvalidDataException("PNML net is missing required 'id' attribute.");
            }

            var session = new NetSession(netId);

            if (reader.IsEmptyElement)
            {
                results.Add(Materialize(session.Builder, target));
                continue;
            }

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("net", StringComparison.Ordinal))
                {
                    break;
                }

                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                if (reader.LocalName.Equals("place", StringComparison.Ordinal))
                {
                    ParsePlace(reader, session);
                    continue;
                }

                if (reader.LocalName.Equals("transition", StringComparison.Ordinal))
                {
                    ParseTransition(reader, session);
                    continue;
                }

                if (reader.LocalName.Equals("arc", StringComparison.Ordinal))
                {
                    ParseArc(reader, session);
                    continue;
                }
            }

            ApplyArcs(session);
            results.Add(Materialize(session.Builder, target));
        }

        return results;
    }

    static bool IsSupportedNetType(string type)
    {
        return string.Equals(type, "P/T net", StringComparison.Ordinal)
            || string.Equals(type, "http://www.example.org/pnml/PTNet", StringComparison.Ordinal);
    }

    static PetriNetBase Materialize(PetriNetBuilder builder, PnmlTargetModel target)
    {
        return target == PnmlTargetModel.Graph
            ? builder.BuildGraph()
            : builder.BuildMatrix();
    }

    static void ParsePlace(XmlReader reader, NetSession session)
    {
        var placeId = reader.GetAttribute("id");
        if (string.IsNullOrWhiteSpace(placeId))
        {
            throw new InvalidDataException($"PNML net '{session.NetId}' has a place without required 'id' attribute.");
        }

        if (!session.PlaceIds.Add(placeId))
        {
            throw new InvalidDataException($"PNML net '{session.NetId}' contains duplicate place id '{placeId}'.");
        }

        session.Builder.AddingPlace(placeId);

        var initialMarking = default(int?);
        var capacity = default(int?);

        using (var subtree = reader.ReadSubtree())
        {
            while (subtree.Read())
            {
                if (subtree.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                if (subtree.LocalName.Equals("initialMarking", StringComparison.Ordinal))
                {
                    initialMarking = ReadNestedInt(subtree, session.NetId, $"place '{placeId}' initial marking");
                    continue;
                }

                if (subtree.LocalName.Equals("capacity", StringComparison.Ordinal))
                {
                    capacity = ReadNestedInt(subtree, session.NetId, $"place '{placeId}' capacity");
                }
            }
        }

        if (initialMarking.HasValue)
        {
            session.Builder.WithInitialMarking(placeId, initialMarking.Value);
        }

        if (capacity.HasValue)
        {
            session.Builder.WithCapacity(placeId, capacity.Value);
        }

    }

    static void ParseTransition(XmlReader reader, NetSession session)
    {
        var transitionId = reader.GetAttribute("id");
        if (string.IsNullOrWhiteSpace(transitionId))
        {
            throw new InvalidDataException($"PNML net '{session.NetId}' has a transition without required 'id' attribute.");
        }

        if (!session.TransitionIds.Add(transitionId))
        {
            throw new InvalidDataException($"PNML net '{session.NetId}' contains duplicate transition id '{transitionId}'.");
        }

        session.Builder.AddingTransition(transitionId);
    }

    static void ParseArc(XmlReader reader, NetSession session)
    {
        var arcId = reader.GetAttribute("id") ?? "<unknown>";
        var source = reader.GetAttribute("source");
        var target = reader.GetAttribute("target");

        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            throw new InvalidDataException($"PNML net '{session.NetId}' arc '{arcId}' is missing required source/target attributes.");
        }

        var weight = 1;
        var isInhibitor = false;

        using (var subtree = reader.ReadSubtree())
        {
            while (subtree.Read())
            {
                if (subtree.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                if (subtree.LocalName.Equals("inscription", StringComparison.Ordinal))
                {
                    weight = ReadNestedInt(subtree, session.NetId, $"arc '{arcId}' inscription", fallback: 1);
                    continue;
                }

                if (subtree.LocalName.Equals("type", StringComparison.Ordinal))
                {
                    var typeValue = subtree.GetAttribute("value") ?? ReadNestedText(subtree);
                    isInhibitor = string.Equals(typeValue, "inhibitor", StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        if (weight <= 0)
        {
            throw new InvalidDataException($"PNML net '{session.NetId}' arc '{arcId}' has invalid weight '{weight}'.");
        }

        session.PendingArcs.Add(new PendingArc(arcId, source, target, weight, isInhibitor));
    }

    static void ApplyArcs(NetSession session)
    {
        for (var i = 0; i < session.PendingArcs.Count; i++)
        {
            var arc = session.PendingArcs[i];
            var sourceIsPlace = session.Builder.ContainsPlace(arc.SourceId);
            var sourceIsTransition = session.Builder.ContainsTransition(arc.SourceId);
            var targetIsPlace = session.Builder.ContainsPlace(arc.TargetId);
            var targetIsTransition = session.Builder.ContainsTransition(arc.TargetId);

            if (sourceIsPlace && targetIsTransition)
            {
                session.Builder.AddingInputArc(arc.SourceId, arc.TargetId, arc.Weight, arc.IsInhibitor);
                continue;
            }

            if (sourceIsTransition && targetIsPlace)
            {
                session.Builder.AddingOutputArc(arc.SourceId, arc.TargetId, arc.Weight);
                continue;
            }

            var missingEndpoint = sourceIsPlace || sourceIsTransition ? arc.TargetId : arc.SourceId;
            throw new KeyNotFoundException(
                $"PNML net '{session.NetId}' arc '{arc.ArcId}' references endpoint id '{missingEndpoint}', but no matching place or transition was loaded.");
        }
    }

    static int ReadNestedInt(XmlReader reader, string netId, string context, int fallback = 0)
    {
        var text = ReadNestedText(reader);
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        if (int.TryParse(text, out var result))
        {
            return result;
        }

        throw new InvalidDataException($"PNML net '{netId}' has invalid integer '{text}' for {context}.");
    }

    static string? ReadNestedText(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            return null;
        }

        var depth = reader.Depth;
        while (reader.Read())
        {
            if ((reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
                && !string.IsNullOrWhiteSpace(reader.Value))
            {
                return reader.Value.Trim();
            }

            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                break;
            }
        }

        return null;
    }

    sealed class NetSession
    {
        public NetSession(string netId)
        {
            NetId = netId;
            Builder = new PetriNetBuilder().WithName(netId);
        }

        public string NetId { get; }
        public PetriNetBuilder Builder { get; }
        public HashSet<string> PlaceIds { get; } = new(StringComparer.Ordinal);
        public HashSet<string> TransitionIds { get; } = new(StringComparer.Ordinal);
        public List<PendingArc> PendingArcs { get; } = new();
    }

    readonly record struct PendingArc(string ArcId, string SourceId, string TargetId, int Weight, bool IsInhibitor);
}
