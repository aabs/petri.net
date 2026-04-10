using System.Text;
using System.Text.RegularExpressions;
using petrinets2.core;
using Xunit;

namespace core.tests;

public class TestArcNotation
{
    private static IEnumerable<object[]> WithCaseIds(IEnumerable<object[]> data, string prefix)
    {
        var i = 1;
        foreach (var row in data)
        {
            yield return new[] { (object)$"{prefix}.{i++}" }.Concat(row).ToArray();
        }
    }

    private static string NormalizeLegacySpec(string spec)
    {
        var normalized = spec.Replace("\r", " ").Replace("\n", " ");
        normalized = Regex.Replace(normalized, @"PetriNet\s+([A-Za-z][A-Za-z0-9]*)\s*:\s*", "PetriNet $1 < ");
        normalized = Regex.Replace(normalized, @">\s*M\s+", "> ");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        if (normalized.Contains("<") && !normalized.Contains(">"))
        {
            normalized += " >";
        }

        return normalized;
    }

    private static CreatePetriNet ParseWithArclang(string legacySpec)
    {
        var spec = NormalizeLegacySpec(legacySpec);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(spec));
        var scanner = new Scanner(stream);
        var parser = new Parser(scanner);

        parser.Parse();

        if (parser.errors.count > 0)
        {
            throw new InvalidOperationException($"Arclang parse failed with {parser.errors.count} error(s).");
        }

        var builder = parser.Builder;

        // Keep legacy invalid duplicate-name behavior.
        if (builder.Places.Values.Intersect(builder.Transitions.Values).Any())
        {
            throw new InvalidOperationException("A symbol cannot be both a place and a transition.");
        }

        return builder;
    }

    private static void RunFullTestCase(string spec, int inArcs, int outArcs, int numInhibitors)
    {
        var pnb = ParseWithArclang(spec);

        var actualInArcs = pnb.InArcs.SelectMany(x => x.Value).Count();
        var actualOutArcs = pnb.OutArcs.SelectMany(x => x.Value).Count();
        var actualInhibitors = pnb.InArcs.SelectMany(x => x.Value).Count(x => x.IsInhibitor);

        Assert.Equal(inArcs, actualInArcs);
        Assert.Equal(outArcs, actualOutArcs);
        Assert.Equal(numInhibitors, actualInhibitors);
    }

    [Theory]
    [MemberData(nameof(GoodDataWithIds))]
    [MemberData(nameof(FullSpecsWithIds))]
    public void LegacyGoodAndFullCasesPass(string caseId, string spec, int inArcs, int outArcs, int numInhibitors)
    {
        _ = caseId;
        RunFullTestCase(spec, inArcs, outArcs, numInhibitors);
    }

    [Theory]
    [MemberData(nameof(BadDataWithIds))]
    public void LegacyBadCasesFail(string caseId, string spec, int inArcs, int outArcs, int numInhibitors)
    {
        _ = caseId;
        _ = inArcs;
        _ = outArcs;
        _ = numInhibitors;
        Assert.ThrowsAny<Exception>(() => ParseWithArclang(spec));
    }

    [Theory]
    [MemberData(nameof(MarkingCasesWithIds))]
    public void LegacyMarkingCasesPass(string caseId, string spec, int p1Tokens, int p2Tokens)
    {
        _ = caseId;
        var pnb = ParseWithArclang(spec);
        var m = pnb.CreateMarking();

        Assert.Equal(p1Tokens, m[pnb.PlaceIndex("p1")]);
        Assert.Equal(p2Tokens, m[pnb.PlaceIndex("p2")]);
    }

    public static IEnumerable<object[]> FullSpecsWithIds() => WithCaseIds(FullSpecs(), "F");

    public static IEnumerable<object[]> GoodDataWithIds() => WithCaseIds(GoodData(), "G");

    public static IEnumerable<object[]> BadDataWithIds() => WithCaseIds(BadData(), "B");

    public static IEnumerable<object[]> MarkingCasesWithIds() => WithCaseIds(MarkingCases(), "M");

    public static IEnumerable<object[]> FullSpecs()
    {
        yield return new object[] { "PetriNet mynet: {p1,p2})-[t1; t1]-(p3; p3)-[t2; t2]-({p1,p2}; ", 3, 3, 0 };
        yield return new object[] { "PetriNet mynet: {p1,p2})-[{t1}; t1]-(p3; p3)-[t2; t2]-({p1,p2}; ", 3, 3, 0 };
    }

    public static IEnumerable<object[]> GoodData()
    {
        yield return new object[] { "PetriNet mynet: p1)[t1; ", 1, 0, 0 };
        yield return new object[] { "PetriNet mynet: p1)-[t1; ", 1, 0, 0 };
        yield return new object[] { "PetriNet mynet: p1)--[t1; ", 1, 0, 0 };
        yield return new object[] { "PetriNet mynet: p1)---------------------[t1; ", 1, 0, 0 };
        yield return new object[] { "PetriNet mynet: p1)o[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)-o[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)-o-[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)----o----[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)2[t1; ", 1, 0, 0 };
        yield return new object[] { "PetriNet mynet: p1)2-[t1; ", 1, 0, 0 };
        yield return new object[] { "PetriNet mynet: p1)-2[t1; ", 1, 0, 0 };
        yield return new object[] { "PetriNet mynet: p1)--2--[t1; ", 1, 0, 0 };
        yield return new object[] { "PetriNet mynet: p1)------------------2--[t1; ", 1, 0, 0 };
        yield return new object[] { "PetriNet mynet: p1)2------------------[t1; ", 1, 0, 0 };
        yield return new object[] { "PetriNet mynet: p1)2o[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)-2o[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)2-o[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)2o-[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)-2-o[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)-2o-[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)-2-o-[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)-2o[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)2--o[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)2o--[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)--2--o[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)--2o--[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: p1)--2--o--[t1; ", 1, 0, 1 };
        yield return new object[] { "PetriNet mynet: t1](p1; ", 0, 1, 0 };
        yield return new object[] { "PetriNet mynet: t1]-(p1; ", 0, 1, 0 };
        yield return new object[] { "PetriNet mynet: t1]------(p1; ", 0, 1, 0 };
        yield return new object[] { "PetriNet mynet: t1]2(p1; ", 0, 1, 0 };
        yield return new object[] { "PetriNet mynet: t1]-2(p1; ", 0, 1, 0 };
        yield return new object[] { "PetriNet mynet: t1]2-(p1; ", 0, 1, 0 };
        yield return new object[] { "PetriNet mynet: t1]-2-(p1; ", 0, 1, 0 };
        yield return new object[] { "PetriNet mynet: t1]--2(p1; ", 0, 1, 0 };
        yield return new object[] { "PetriNet mynet: t1]2--(p1; ", 0, 1, 0 };
        yield return new object[] { "PetriNet mynet: t1]--2--(p1; ", 0, 1, 0 };
    }

    public static IEnumerable<object[]> BadData()
    {
        yield return new object[] { "PetriNet mynet: p1)[p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)-[p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)--[p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)-2-[p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)-2-o-[p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1]-(t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1)(p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1)-(p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1)--(p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1)-2-(p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1)-2-o-(p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1][p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1]-[p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1]--[p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1]-2-[p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1]-2-o-[p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )-[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )--[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )---------------------[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )o[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )-o[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )-o-[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )----o----[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )2[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )2-[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )-2[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )--2--[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )------------------2--[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )2------------------[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )2o[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )-2o[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )2-o[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )2o-[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )-2-o[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )-2o-[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )-2-o-[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )-2o[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )2--o[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )2o--[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )--2--o[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )--2o--[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: )--2--o--[t1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: ](p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: ]-(p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: ]------(p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: ]2(p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: ]-2(p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: ]2-(p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: ]-2-(p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: ]--2(p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: ]2--(p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: ]--2--(p1; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)-[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)--[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)---------------------[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)o[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)-o[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)-o-[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)----o----[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)2[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)2-[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)-2[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)--2--[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)------------------2--[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)2------------------[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)2o[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)-2o[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)2-o[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)2o-[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)-2-o[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)-2o-[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)-2-o-[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)-2o[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)2--o[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)2o--[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)--2--o[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)--2o--[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: p1)--2--o--[; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1](; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1]-(; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1]------(; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1]2(; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1]-2(; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1]2-(; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1]-2-(; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1]--2(; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1]2--(; ", 1, 1, 1 };
        yield return new object[] { "PetriNet mynet: t1]--2--(; ", 1, 1, 1 };
    }

    public static IEnumerable<object[]> MarkingCases()
    {
        yield return new object[]
        {
            @"
PetriNet mynet
<
    {p1,p2})-[t1;
    t1]-(p3;
    p3)-[t2;
    t2]-({p1,p2};
>
M p1=2;p2=3; ",
            2,
            3
        };
    }
}
