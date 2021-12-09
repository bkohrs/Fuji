namespace Fuji;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ProvidedByCollectionAttribute : Attribute
{
    public ProvidedByCollectionAttribute(Type interfaceType)
    {
        InterfaceType = interfaceType;
    }

    public Type InterfaceType { get; }
}