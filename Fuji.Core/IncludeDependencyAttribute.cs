namespace Fuji;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class IncludeDependencyAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;
}