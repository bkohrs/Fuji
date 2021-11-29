namespace Fuji;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ProvideTransientAttribute : Attribute
{
    public ProvideTransientAttribute(Type interfaceType, Type? implementationType = null)
    {
        ImplementationType = implementationType ?? interfaceType;
        InterfaceType = interfaceType;
    }

    public Type ImplementationType { get; }
    public Type InterfaceType { get; }
}