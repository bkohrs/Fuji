using Microsoft.CodeAnalysis;

namespace Fuji;

public record struct AttributedSymbol(INamedTypeSymbol Symbol, AttributeData Attribute);