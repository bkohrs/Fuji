namespace Fuji.Generated;

public class FactoryProvidedSingleton
{
    public FactoryProvidedSingleton() : this(false)
    {
    }

    public FactoryProvidedSingleton(bool factoryProvided)
    {
        FactoryProvided = factoryProvided;
    }

    public bool FactoryProvided { get; }
}