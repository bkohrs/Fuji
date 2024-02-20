namespace Fuji;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ProvideSingletonAttribute : Attribute
{
    public ProvideSingletonAttribute(Type interfaceType, Type? implementationType = null)
    {
        ImplementationType = implementationType ?? interfaceType;
        InterfaceType = interfaceType;
    }

    public Type ImplementationType { get; }
    public Type InterfaceType { get; }
    public string? Factory { get; set; }
    public string? Key { get; set; }
    public int Priority { get; set; }
}