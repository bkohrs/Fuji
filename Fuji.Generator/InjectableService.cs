using Microsoft.CodeAnalysis;

namespace Fuji;

public record struct InjectableService(
    INamedTypeSymbol InterfaceType,
    INamedTypeSymbol ImplementationType,
    ServiceLifetime Lifetime,
    IMethodSymbol? CustomFactory,
    int Priority,
    bool HasObsoleteAttribute);