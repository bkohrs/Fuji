using JetBrains.Annotations;

namespace Fuji.Generated;

[UsedImplicitly]
public class ScopedServiceWithTransientDependency
{
    public ScopedServiceWithTransientDependency(ITransientService transientService)
    {
        TransientService = transientService;
    }

    public ITransientService TransientService { get; }
}