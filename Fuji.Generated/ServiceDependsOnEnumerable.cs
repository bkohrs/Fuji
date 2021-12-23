namespace Fuji.Generated;

public class ServiceDependsOnEnumerable
{
    public ServiceDependsOnEnumerable(IEnumerable<IMultipleImplementationService> dependencies)
    {
        Dependencies = dependencies;
    }

    public IEnumerable<IMultipleImplementationService> Dependencies { get; }
}