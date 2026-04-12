namespace core.tests;

public static class TransitionSelectionPropertyData
{
    public static (GraphPetriNet Graph, MatrixPetriNet Matrix, Marking Marking) CreateEquivalentNetPair(bool conflict, int leftPriority, int rightPriority)
    {
        var placeNames = conflict
            ? new Dictionary<int, string> { [0] = "shared", [1] = "leftTarget", [2] = "rightTarget" }
            : new Dictionary<int, string> { [0] = "leftSource", [1] = "rightSource", [2] = "leftTarget", [3] = "rightTarget" };

        var transitionNames = new Dictionary<int, string> { [0] = "t0", [1] = "t1" };
        var transitionOrdering = new Dictionary<int, int> { [0] = leftPriority, [1] = rightPriority };

        Dictionary<int, List<InArc>> inArcs;
        Dictionary<int, List<OutArc>> outArcs;
        Marking marking;

        if (conflict)
        {
            inArcs = new Dictionary<int, List<InArc>>
            {
                [0] = new List<InArc> { new(0, 1, false) },
                [1] = new List<InArc> { new(0, 1, false) }
            };

            outArcs = new Dictionary<int, List<OutArc>>
            {
                [0] = new List<OutArc> { new(1, 1) },
                [1] = new List<OutArc> { new(2, 1) }
            };

            marking = ToMarking(1, 0, 0);
        }
        else
        {
            inArcs = new Dictionary<int, List<InArc>>
            {
                [0] = new List<InArc> { new(0, 1, false) },
                [1] = new List<InArc> { new(1, 1, false) }
            };

            outArcs = new Dictionary<int, List<OutArc>>
            {
                [0] = new List<OutArc> { new(2, 1) },
                [1] = new List<OutArc> { new(3, 1) }
            };

            marking = ToMarking(1, 1, 0, 0);
        }

        return (
            new GraphPetriNet("graph", placeNames, transitionNames, inArcs, outArcs, transitionOrdering),
            new MatrixPetriNet("matrix", placeNames, transitionNames, inArcs, outArcs, transitionOrdering),
            marking);
    }

    public static int[] Snapshot(Marking marking)
    {
        return Enumerable.Range(0, marking.Size).Select(index => marking[index]).ToArray();
    }

    public static Marking ToMarking(params int[] values)
    {
        return new Marking(values);
    }
}