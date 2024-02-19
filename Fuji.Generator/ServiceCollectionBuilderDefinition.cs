using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Fuji;

public class ServiceCollectionBuilderDefinition
{
    public ServiceCollectionBuilderDefinition(INamedTypeSymbol serviceCollectionBuilderType, ImmutableArray<InjectableService> providedServices)
    {
        ServiceCollectionBuilderType = serviceCollectionBuilderType;
        ProvidedServices = providedServices;
    }

    public INamedTypeSymbol ServiceCollectionBuilderType { get; set; }
    public ImmutableArray<InjectableService> ProvidedServices { get; set; }
}