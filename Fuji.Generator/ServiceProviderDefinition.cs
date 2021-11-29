using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Fuji;

public class ServiceProviderDefinition
{
    public ServiceProviderDefinition(INamedTypeSymbol serviceProviderType, ImmutableArray<InjectableService> providedServices, string? debugOutputPath)
    {
        ServiceProviderType = serviceProviderType;
        ProvidedServices = providedServices;
        DebugOutputPath = debugOutputPath ?? "";
    }

    public INamedTypeSymbol ServiceProviderType { get; set; }
    public ImmutableArray<InjectableService> ProvidedServices { get; set; }
    public string DebugOutputPath { get; set; }
}