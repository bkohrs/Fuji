namespace Fuji;

[AttributeUsage(AttributeTargets.Class)]
public class ServiceCollectionBuilderAttribute : Attribute
{
    public string? DebugOutputPath { get; set; }
    public bool IncludeAllServices { get; set; }
}