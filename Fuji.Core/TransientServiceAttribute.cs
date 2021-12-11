namespace Fuji;

[AttributeUsage(AttributeTargets.Class)]
public class TransientServiceAttribute : Attribute
{
    public TransientServiceAttribute(Type? interfaceType = null)
    {
        InterfaceType = interfaceType;
    }

    public Type? InterfaceType { get; }
}