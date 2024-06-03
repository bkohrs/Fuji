namespace Fuji;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SingletonServiceAttribute : Attribute
{
    public SingletonServiceAttribute(Type? interfaceType = null)
    {
        InterfaceType = interfaceType;
    }

    public Type? InterfaceType { get; }
    public string? Key { get; set; }
    public int Priority { get; set; }
}