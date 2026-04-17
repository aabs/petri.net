namespace core.tests;

public class ScratchBufferFailureModeProperties
{
    [Property]
    public bool FallbackExhaustion_FailsAtomically(PositiveInt transitionSeed)
    {
        var transitionCount = (transitionSeed.Get % 32) + 1;
        var scenario = ScratchBufferPoolingPropertyData.CreateFireCase(transitionCount);

        return ScratchBufferPoolingOracle.ExhaustionIsAtomic(scenario);
    }

    [Property]
    public bool RetryBackoff_AttemptBudgetIsFive(PositiveInt prioritySeed)
    {
        var scenario = ScratchBufferPoolingPropertyData.CreatePlanningCase(
            conflict: true,
            leftPrioritySeed: prioritySeed.Get % 16,
            rightPrioritySeed: 0);

        return ScratchBufferPoolingOracle.RetryBudgetIsEnforced(scenario);
    }

    [Property]
    public bool DefaultCapAndOverrideBounds_AreEnforced(PositiveInt seed)
    {
        var minCap = ScratchPoolConfiguration.MinPerThreadCapacityCapBytes;
        var maxCap = ScratchPoolConfiguration.MaxPerThreadCapacityCapBytes;

        var defaultIsExpected = ScratchBufferPooling.EffectivePerThreadCapacityCapBytes == ScratchPoolConfiguration.DefaultPerThreadCapacityCapBytes;
        using var lowScope = ScratchBufferPooling.BeginTestScope(enablePooling: true, perThreadCapacityCapBytes: seed.Get % minCap);
        var lowClamped = ScratchBufferPooling.EffectivePerThreadCapacityCapBytes == minCap;

        using var highScope = ScratchBufferPooling.BeginTestScope(enablePooling: true, perThreadCapacityCapBytes: maxCap + seed.Get + 1);
        var highClamped = ScratchBufferPooling.EffectivePerThreadCapacityCapBytes == maxCap;

        return defaultIsExpected && lowClamped && highClamped;
    }
}
