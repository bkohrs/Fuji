namespace Fuji.Generated;

public class TransientServiceWithScopedDependency
{
    public TransientServiceWithScopedDependency(IScopedService scopedService)
    {
        ScopedService = scopedService;
    }

    public IScopedService ScopedService { get; }
}