namespace core.tests;

public static class ScratchBufferPoolingOracle
{
    public static bool PlanningParityIsPreserved(ScratchBufferPlanningCase scenario)
    {
        using var baselineScope = ScratchBufferPooling.BeginTestScope(enablePooling: false);
        var baselineGraph = scenario.Graph.CreateFiringPlan(scenario.Marking).TransitionIds.ToArray();
        var baselineMatrix = scenario.Matrix.CreateFiringPlan(scenario.Marking).TransitionIds.ToArray();

        using var pooledScope = ScratchBufferPooling.BeginTestScope(enablePooling: true);
        var pooledGraph = scenario.Graph.CreateFiringPlan(scenario.Marking).TransitionIds.ToArray();
        var pooledMatrix = scenario.Matrix.CreateFiringPlan(scenario.Marking).TransitionIds.ToArray();

        return baselineGraph.SequenceEqual(pooledGraph)
            && baselineMatrix.SequenceEqual(pooledMatrix)
            && pooledGraph.SequenceEqual(pooledMatrix);
    }

    public static bool FiringParityIsPreserved(ScratchBufferFireCase scenario)
    {
        using var baselineScope = ScratchBufferPooling.BeginTestScope(enablePooling: false);
        var baselineGraph = scenario.Graph.Fire(scenario.Marking);
        var baselineMatrix = scenario.Matrix.Fire(scenario.Marking);

        using var pooledScope = ScratchBufferPooling.BeginTestScope(enablePooling: true);
        var pooledGraph = scenario.Graph.Fire(scenario.Marking);
        var pooledMatrix = scenario.Matrix.Fire(scenario.Marking);

        return ScratchBufferPoolingPropertyData.Snapshot(baselineGraph).SequenceEqual(ScratchBufferPoolingPropertyData.Snapshot(pooledGraph))
            && ScratchBufferPoolingPropertyData.Snapshot(baselineMatrix).SequenceEqual(ScratchBufferPoolingPropertyData.Snapshot(pooledMatrix))
            && ScratchBufferPoolingPropertyData.Snapshot(pooledGraph).SequenceEqual(ScratchBufferPoolingPropertyData.Snapshot(pooledMatrix));
    }

    public static bool SteadyStateReuseObserved(ScratchBufferFireCase scenario, int cycles)
    {
        ScratchBufferPooling.ResetDiagnostics();
        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: true);

        for (var i = 0; i < cycles; i++)
        {
            scenario.Graph.Fire(scenario.Marking);
            scenario.Matrix.Fire(scenario.Marking);
            scenario.Graph.CreateFiringPlan(scenario.Marking);
            scenario.Matrix.CreateFiringPlan(scenario.Marking);
        }

        var diagnostics = ScratchBufferPooling.GetDiagnosticsSnapshot();
        return diagnostics.PlanningFreshAllocations < diagnostics.PlanningRentals
            && diagnostics.DeltaFreshAllocations < diagnostics.DeltaRentals;
    }

    public static bool NoCrossCallContamination(ScratchBufferFireCase scenario)
    {
        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: true);

        var first = scenario.Graph.Fire(scenario.Marking);
        var second = scenario.Graph.Fire(scenario.Marking);

        return ScratchBufferPoolingPropertyData.Snapshot(first).SequenceEqual(ScratchBufferPoolingPropertyData.Snapshot(second))
            && ScratchBufferPoolingPropertyData.Snapshot(scenario.Marking).SequenceEqual(ScratchBufferPoolingPropertyData.Snapshot(scenario.Marking));
    }

    public static bool DoubleReturnIsPrevented()
    {
        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: true);
        var lease = ScratchBufferPooling.AcquirePlanningScratch();
        lease.Dispose();

        try
        {
            lease.Dispose();
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    public static bool LeaseReleaseOccursOnExceptionPath()
    {
        ScratchBufferPooling.ResetDiagnostics();
        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: true);

        try
        {
            FiringPlanner.Create(new[] { 0, 1 }, true, _ => throw new InvalidOperationException("forced"));
        }
        catch (InvalidOperationException)
        {
            // expected
        }

        var diagnostics = ScratchBufferPooling.GetDiagnosticsSnapshot();
        return diagnostics.PlanningOutstandingLeases == 0 && diagnostics.DeltaOutstandingLeases == 0;
    }

    public static bool RetryBudgetIsEnforced(ScratchBufferPlanningCase scenario)
    {
        ScratchBufferPooling.ResetDiagnostics();
        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: true, forceExhaustion: true, retryCount: 5);

        try
        {
            scenario.Graph.CreateFiringPlan(scenario.Marking);
            return false;
        }
        catch (ScratchBufferAcquisitionException)
        {
            var diagnostics = ScratchBufferPooling.GetDiagnosticsSnapshot();
            return diagnostics.RetryAttempts == 5 && diagnostics.AcquisitionFailures == 1;
        }
    }

    public static bool ExhaustionIsAtomic(ScratchBufferFireCase scenario)
    {
        var before = ScratchBufferPoolingPropertyData.Snapshot(scenario.Marking);

        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: true, forceExhaustion: true, retryCount: 5);
        try
        {
            scenario.Graph.Fire(scenario.Marking);
            return false;
        }
        catch (ScratchBufferAcquisitionException)
        {
            var after = ScratchBufferPoolingPropertyData.Snapshot(scenario.Marking);
            return before.SequenceEqual(after);
        }
    }
}
