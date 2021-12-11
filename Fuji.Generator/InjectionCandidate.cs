using Microsoft.CodeAnalysis;

namespace Fuji;

public record struct InjectionCandidate(
    INamedTypeSymbol InterfaceType,
    INamedTypeSymbol ImplementationType,
    ServiceLifetime Lifetime,
    IMethodSymbol? CustomFactory);