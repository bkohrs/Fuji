namespace Fuji.Generated;

public class TransientServiceWithSingletonDependency
{
    public TransientServiceWithSingletonDependency(ISingletonService singletonService)
    {
        SingletonService = singletonService;
    }

    public ISingletonService SingletonService { get; }
}