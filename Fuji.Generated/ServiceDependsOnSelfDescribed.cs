using JetBrains.Annotations;

namespace Fuji.Generated;

public class ServiceDependsOnSelfDescribed
{
    public ServiceDependsOnSelfDescribed(
        [UsedImplicitly] SelfDescribedTransientService transientService,
        [UsedImplicitly] SelfDescribedSingletonService singletonService,
        [UsedImplicitly] SelfDescribedScopedService scopedService)
    {
    }
}