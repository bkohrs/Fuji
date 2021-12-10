using JetBrains.Annotations;

namespace Fuji.Generated;

[UsedImplicitly]
public class ScopedServiceWithSingletonDependency
{
    public ScopedServiceWithSingletonDependency(ISingletonService singletonService)
    {
        SingletonService = singletonService;
    }

    public ISingletonService SingletonService { get; }
}