namespace Fuji.Generated;

public class FactoryProvidedSingletonNeedingServiceProvider
{
    public FactoryProvidedSingletonNeedingServiceProvider() : this(false)
    {
    }

    public FactoryProvidedSingletonNeedingServiceProvider(bool factoryProvided)
    {
        FactoryProvided = factoryProvided;
    }

    public bool FactoryProvided { get; }
}