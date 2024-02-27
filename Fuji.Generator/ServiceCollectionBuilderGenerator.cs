using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fuji;

[Generator]
public class ServiceCollectionBuilderGenerator : IIncrementalGenerator
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

        var processor = new ServiceCollectionBuilderProcessor(compilation, sourceProductionContext);
        processor.Process(distinctTypes);
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeDeclarationSyntaxes = context.SyntaxProvider.CreateSyntaxProvider<TypeDeclarationSyntax?>(
                static (syntaxNode, _) => syntaxNode is TypeDeclarationSyntax,
                static (generatorSyntaxContext, _) => generatorSyntaxContext.Node as TypeDeclarationSyntax)
            .Where(node => node is not null);
        var compilationAndTypes = context.CompilationProvider.Combine(typeDeclarationSyntaxes.Collect());
        context.RegisterSourceOutput(compilationAndTypes, (sourceProductionContext, tuple) =>
            Generate(sourceProductionContext, tuple.Item1, tuple.Item2));
    }
}