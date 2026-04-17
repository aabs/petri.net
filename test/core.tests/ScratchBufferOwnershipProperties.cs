namespace core.tests;

public class ScratchBufferOwnershipProperties
{
    [Property]
    public bool NoCrossCallContamination_IsMaintained(PositiveInt transitionSeed)
    {
        var transitionCount = (transitionSeed.Get % 64) + 1;
        var scenario = ScratchBufferPoolingPropertyData.CreateFireCase(transitionCount);

        return ScratchBufferPoolingOracle.NoCrossCallContamination(scenario);
    }

    [Property]
    public bool DoubleReturnInvariant_IsEnforced()
    {
        return ScratchBufferPoolingOracle.DoubleReturnIsPrevented();
    }

    [Property]
    public bool LeaseReleaseOccurs_OnExceptionPaths()
    {
        return ScratchBufferPoolingOracle.LeaseReleaseOccursOnExceptionPath();
    }
}
