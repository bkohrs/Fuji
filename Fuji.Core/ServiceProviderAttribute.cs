namespace Fuji;

[AttributeUsage(AttributeTargets.Class)]
public class ServiceProviderAttribute : Attribute
{
    public bool IncludeAllServices { get; set; }
}