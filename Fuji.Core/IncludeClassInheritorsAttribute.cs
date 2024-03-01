namespace Fuji;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class IncludeClassInheritorsAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;
}