global using System;
global using System.Collections.Generic;
global using System.Linq;
global using FsCheck;
global using FsCheck.Xunit;
global using petrinets2.core;
global using Xunit;

[assembly: Properties(Arbitrary = new[] { typeof(core.tests.ScratchBufferPoolingArbitraries) })]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace core.tests;

public static class ScratchBufferPoolingArbitraries
{
    public static Arbitrary<int> TransitionCount()
    {
        var generator = FsCheck.Fluent.Gen.Choose(1, 256);
        return FsCheck.Fluent.Arb.From(generator);
    }

    public static Arbitrary<int> WorkerCount()
    {
        var generator = FsCheck.Fluent.Gen.Choose(2, 16);
        return FsCheck.Fluent.Arb.From(generator);
    }
}
