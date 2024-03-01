namespace Fuji;

[AttributeUsage(AttributeTargets.Class)]
public class ServiceCollectionBuilderAttribute : Attribute
{
    public bool IncludeAllServices { get; set; }
}