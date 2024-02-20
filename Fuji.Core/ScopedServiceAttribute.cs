namespace Fuji;

[AttributeUsage(AttributeTargets.Class)]
public class ScopedServiceAttribute : Attribute
{
    public ScopedServiceAttribute(Type? interfaceType = null)
    {
        InterfaceType = interfaceType;
    }

    public Type? InterfaceType { get; }
    public string? Key { get; set; }
    public int Priority { get; set; }
}