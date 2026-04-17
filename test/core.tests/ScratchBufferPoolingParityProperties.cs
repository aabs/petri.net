namespace core.tests;

public class ScratchBufferPoolingParityProperties
{
    [Property]
    public bool PlanningOutputParity_IsPreservedUnderReuse(bool conflict, NonNegativeInt leftPrioritySeed, NonNegativeInt rightPrioritySeed)
    {
        var scenario = ScratchBufferPoolingPropertyData.CreatePlanningCase(
            conflict,
            leftPrioritySeed.Get % 16,
            rightPrioritySeed.Get % 16);

        return ScratchBufferPoolingOracle.PlanningParityIsPreserved(scenario);
    }

    [Property]
    public bool FiringOutputParity_IsPreservedUnderReuse(PositiveInt transitionSeed)
    {
        var transitionCount = (transitionSeed.Get % 64) + 1;
        var scenario = ScratchBufferPoolingPropertyData.CreateFireCase(transitionCount);

        return ScratchBufferPoolingOracle.FiringParityIsPreserved(scenario);
    }
}
