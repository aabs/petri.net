# petri.net

This repository is a progressive modernization of the original [aabs/PetriNets](https://github.com/aabs/PetriNets) project (first created in 2009).

The goal is to keep the core Petri Net modeling approach intact while updating implementation quality, tests, and runtime behavior for current .NET.

## What You Can Do Here

- Build Place/Transition nets using a fluent builder.
- Create either graph-backed or matrix-backed net models from the same definition.
- Run markings through repeated transition firing.
- Load PNML models into `GraphPetriNet` or `MatrixPetriNet`.

## Project Layout

- `src/core`: Core Petri Net models and builders.
- `src/cli`: CLI project scaffold.
- `test/core.tests`: Unit and property tests.
- `docs`: Notes, plans, and performance work items.

## Build And Test

Prerequisite: .NET SDK 10.0+

```bash
dotnet build petrinets2.slnx
dotnet test petrinets2.slnx
```

## Quick Start: Define A Net Once, Materialize Either Model

Both `GraphPetriNet` and `MatrixPetriNet` can be created from the same `CreatePetriNet` builder.

```csharp
using petrinets2.core;

var builder = CreatePetriNet
    .Called("demo")
    .WithPlaces("p0", "p1")
    .WithTransitions("t0")
    .With("t0").FedBy("p0").Done()
    .With("t0").Feeding("p1").Done()
    .WithPlace("p0").HavingMarking(1).Done();

var graph = builder.CreateNet<GraphPetriNet>();
var matrix = builder.CreateNet<MatrixPetriNet>();
var marking = builder.CreateMarking();
```

## GraphPetriNet Example

Use `GraphPetriNet` when you want direct arc lists and convenient mutation APIs.

```csharp
using petrinets2.core;

var builder = CreatePetriNet
    .Called("graph-example")
    .WithPlaces("input", "output")
    .WithTransitions("move")
    .With("move").FedBy("input").Done()
    .With("move").Feeding("output").Done()
    .WithPlace("input").HavingMarking(2).Done();

var net = builder.CreateNet<GraphPetriNet>();
var marking = builder.CreateMarking();

var moveId = builder.TransitionIndex("move");
var inputId = builder.PlaceIndex("input");
var outputId = builder.PlaceIndex("output");

while (net.IsEnabled(moveId, marking))
{
    marking = net.Fire(marking);
}

Console.WriteLine($"input={marking[inputId]}, output={marking[outputId]}");
// input=0, output=2
```

## MatrixPetriNet Example

Use `MatrixPetriNet` when you want matrix-based flow calculations and compact transition evaluation.

```csharp
using petrinets2.core;

var places = new Dictionary<int, string>
{
    [0] = "p0",
    [1] = "p1"
};

var transitions = new Dictionary<int, string>
{
    [0] = "t0"
};

var inArcs = new Dictionary<int, List<InArc>>
{
    [0] = new() { new InArc(0, weight: 1, inhibitor: false) }
};

var outArcs = new Dictionary<int, List<OutArc>>
{
    [0] = new() { new OutArc(1, weight: 1) }
};

var net = new MatrixPetriNet("matrix-example", places, transitions, inArcs, outArcs);

var marking = new Marking(2);
marking[0] = 3;
marking[1] = 0;

while (net.IsEnabled(0, marking))
{
    marking = net.Fire(marking);
}

Console.WriteLine($"p0={marking[0]}, p1={marking[1]}");
// p0=0, p1=3
```

## Load PNML Models

The generic PNML loader can materialize either model type.

```csharp
using petrinets2.core;

var graphLoader = new NewPnmlLoader<GraphPetriNet>();
var graphNets = graphLoader.Load("model.pnml").ToList();

var matrixLoader = new NewPnmlLoader<MatrixPetriNet>();
var matrixNets = matrixLoader.Load("model.pnml").ToList();
```

## Notes On Current Direction

- Runtime target is idiomatic C# 14 on `net10.0`.
- Correctness is the primary design objective; performance work follows once behavior is preserved and measurable.
- Tests are property-based and use FsCheck with FsCheck.Xunit under red-green-refactor discipline.
- Method contracts should be expressed with idiomatic C# constructs rather than the deprecated Code Contracts library.
- The repository includes performance remediation notes in `docs/performance-remediation-plan.md`.

If you are migrating from the 2009 codebase, start by defining one net with `CreatePetriNet`, then instantiate both models and compare behavior under the same marking progression.