using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Fuji;

public class ServiceProviderDefinition
{
    public ServiceProviderDefinition(INamedTypeSymbol serviceProviderType, ImmutableArray<InjectableService> providedServices)
    {
        ServiceProviderType = serviceProviderType;
        ProvidedServices = providedServices;
    }

    public INamedTypeSymbol ServiceProviderType { get; set; }
    public ImmutableArray<InjectableService> ProvidedServices { get; set; }
}