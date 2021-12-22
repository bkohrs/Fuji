namespace Fuji;

[AttributeUsage(AttributeTargets.Class)]
public class ScopedServiceAttribute : Attribute
{
    public ScopedServiceAttribute(Type? interfaceType = null)
    {
        InterfaceType = interfaceType;
    }

    public Type? InterfaceType { get; }
    public int Priority { get; set; }
}