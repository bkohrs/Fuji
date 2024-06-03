namespace Fuji;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class TransientServiceAttribute : Attribute
{
    public TransientServiceAttribute(Type? interfaceType = null)
    {
        InterfaceType = interfaceType;
    }

    public Type? InterfaceType { get; }
    public string? Key { get; set; }
    public int Priority { get; set; }
}