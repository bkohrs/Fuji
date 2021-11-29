using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Fuji;

public record struct InjectableService(
    INamedTypeSymbol InterfaceType,
    INamedTypeSymbol ImplementationType,
    ImmutableArray<INamedTypeSymbol> ConstructorArguments);