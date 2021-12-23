using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fuji;

[Generator]
public class ServiceProviderGenerator : IIncrementalGenerator
{
    private void Generate(SourceProductionContext sourceProductionContext, Compilation compilation,
        ImmutableArray<TypeDeclarationSyntax?> typeDeclarationSyntaxes)
    {
        if (typeDeclarationSyntaxes.IsDefaultOrEmpty)
            return;

        var distinctTypes = typeDeclarationSyntaxes
            .Where(typeSyntax => typeSyntax is not null)
            .Cast<TypeDeclarationSyntax>()
            .Distinct()
            .ToImmutableArray();

        var processor = new ServiceProviderProcessor(compilation, sourceProductionContext);
        processor.Process(distinctTypes);
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeDeclarationSyntaxes = context.SyntaxProvider.CreateSyntaxProvider<TypeDeclarationSyntax?>(
            static (syntaxNode, _) =>
            {
                if (syntaxNode is TypeDeclarationSyntax typeDeclarationSyntax)
                {
                    return typeDeclarationSyntax.AttributeLists.Count > 0;
                }
                return false;
            },
            static (generatorSyntaxContext, _) =>
            {
                if (generatorSyntaxContext.Node is TypeDeclarationSyntax typeDeclarationSyntax)
                {
                    foreach (var attributeListSyntax in typeDeclarationSyntax.AttributeLists)
                    {
                        foreach (var attributeSyntax in attributeListSyntax.Attributes)
                        {
                            var attributeTypeSymbol = generatorSyntaxContext.SemanticModel
                                .GetSymbolInfo(attributeSyntax).Symbol?.ContainingType;
                            var attributeDisplayName = attributeTypeSymbol?.ToDisplayString();
                            if (attributeDisplayName == AttributeNames.ServiceProvider ||
                                attributeDisplayName == AttributeNames.ServiceCollectionBuilder ||
                                attributeDisplayName == AttributeNames.TransientService ||
                                attributeDisplayName == AttributeNames.ScopedService ||
                                attributeDisplayName == AttributeNames.SingletonService)
                            {
                                return typeDeclarationSyntax;
                            }
                        }
                    }
                }
                return null;
            }).Where(node => node is not null);
        var compilationAndTypes = context.CompilationProvider.Combine(typeDeclarationSyntaxes.Collect());
        context.RegisterSourceOutput(compilationAndTypes, (sourceProductionContext, tuple) =>
            Generate(sourceProductionContext, tuple.Item1, tuple.Item2));
    }
}