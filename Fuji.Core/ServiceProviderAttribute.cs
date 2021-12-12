namespace Fuji;

[AttributeUsage(AttributeTargets.Class)]
public class ServiceProviderAttribute : Attribute
{
    public string? DebugOutputPath { get; set; }
    public bool IncludeAllServices { get; set; }
}