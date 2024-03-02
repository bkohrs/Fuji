namespace Fuji;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PropertyInjectionTypeAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;
}