using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Fuji;

public record struct InjectableService(
    INamedTypeSymbol InterfaceType,
    INamedTypeSymbol ImplementationType,
    ServiceLifetime Lifetime,
    ImmutableArray<INamedTypeSymbol> ConstructorArguments,
    DisposeType DisposeType,
    IMethodSymbol? CustomFactory,
    int Priority,
    bool HasObsoleteAttribute);