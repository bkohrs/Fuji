namespace Fuji;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ProvideScopedAttribute : Attribute
{
    public ProvideScopedAttribute(Type interfaceType, Type? implementationType = null)
    {
        ImplementationType = implementationType ?? interfaceType;
        InterfaceType = interfaceType;
    }

    public Type ImplementationType { get; }
    public Type InterfaceType { get; }
    public string? Key { get; set; }
    public int Priority { get; set; }
}