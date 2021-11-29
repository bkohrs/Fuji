using Microsoft.CodeAnalysis;

namespace Fuji;

public static class CompilationExtensions
{
    public static INamedTypeSymbol GetRequiredTypeByMetadataName(this Compilation compilation, string metadataName)
    {
        return compilation.GetTypeByMetadataName(metadataName) ??
               throw new ArgumentException($"Unable to get type for {metadataName}.");
    }
}