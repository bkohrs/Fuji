namespace Fuji;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class IncludeInterfaceImplementorsAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;
}