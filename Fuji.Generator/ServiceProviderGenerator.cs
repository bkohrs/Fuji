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

        var asyncDisposableSymbol = compilation.GetRequiredTypeByMetadataName("System.IAsyncDisposable");
        var disposableSymbol = compilation.GetRequiredTypeByMetadataName("System.IDisposable");
        var serviceProviderAttributeType = compilation.GetRequiredTypeByMetadataName(AttributeNames.ServiceProvider);
        var serviceCollectionBuilderAttributeType = compilation.GetRequiredTypeByMetadataName(AttributeNames.ServiceCollectionBuilder);
        var provideTransientAttribute = compilation.GetRequiredTypeByMetadataName(AttributeNames.ProvideTransient);
        var provideAttributes = ImmutableArray.Create(provideTransientAttribute);
        var distinctTypes = typeDeclarationSyntaxes
            .Where(typeSyntax => typeSyntax is not null)
            .Cast<TypeDeclarationSyntax>()
            .Distinct()
            .ToImmutableArray();

        var partitionedTypes = ResolveTypes(compilation, distinctTypes,
                ImmutableArray.Create(serviceProviderAttributeType, serviceCollectionBuilderAttributeType))
            .ToLookup(type => type.Attribute.AttributeClass, SymbolEqualityComparer.Default);

        foreach (var provider in partitionedTypes[serviceProviderAttributeType])
        {
            GenerateCode(sourceProductionContext, provider, provideAttributes,
                asyncDisposableSymbol, disposableSymbol,
                definition => new SourceCodeGenerator(definition).GenerateServiceProvider());
        }
        foreach (var provider in partitionedTypes[serviceCollectionBuilderAttributeType])
        {
            GenerateCode(sourceProductionContext, provider, provideAttributes,
                asyncDisposableSymbol, disposableSymbol,
                definition => new SourceCodeGenerator(definition).GenerateServiceCollectionBuilder());
        }
    }

    private void GenerateCode(SourceProductionContext sourceProductionContext, AttributedSymbol provider,
        ImmutableArray<INamedTypeSymbol> provideAttributes,
        INamedTypeSymbol asyncDisposableSymbol, INamedTypeSymbol disposableSymbol,
        Func<ServiceProviderDefinition, string> generateContent)
    {
        var injectionCandidates =
            GetInjectionCandidates(provider.Symbol, provideAttributes);
        var injectableServices = GetInjectableServices(injectionCandidates, asyncDisposableSymbol, disposableSymbol);
        var debugOutputPath =
            provider.Attribute.NamedArguments.Where(arg => arg.Key == "DebugOutputPath")
                .Select(arg => arg.Value.Value).FirstOrDefault() as string;
        var definition = new ServiceProviderDefinition(provider.Symbol, injectableServices,
            debugOutputPath);

        var fileContent = generateContent(definition);
        var fileName = $"{definition.ServiceProviderType.ToDisplayString()}.generated.cs";
        if (!string.IsNullOrWhiteSpace(definition.DebugOutputPath))
        {
            if (!Directory.Exists(definition.DebugOutputPath))
                Directory.CreateDirectory(definition.DebugOutputPath);
            File.WriteAllText(Path.Combine(definition.DebugOutputPath, fileName), fileContent);
        }
        sourceProductionContext.AddSource(fileName, fileContent);
    }

    private ImmutableArray<InjectableService> GetInjectableServices(
        ImmutableArray<InjectionCandidate> injectionCandidates, INamedTypeSymbol asyncDisposableSymbol,
        INamedTypeSymbol disposableSymbol)
    {
        var validServices = injectionCandidates
            .Select(candidate => candidate.InterfaceType)
            .ToImmutableHashSet(SymbolEqualityComparer.Default);
        return injectionCandidates
            .Select<InjectionCandidate, InjectableService?>(candidate =>
            {
                var constructor = candidate.ImplementationType.Constructors
                    .OrderByDescending(ctor => ctor.Parameters.Length)
                    .FirstOrDefault(ctor =>
                        ctor.Parameters.All(parameter => validServices.Contains(parameter.Type)));

                var disposeType = DisposeType.None;
                if (candidate.ImplementationType.AllInterfaces.Any(symbol =>
                        SymbolEqualityComparer.Default.Equals(symbol, asyncDisposableSymbol)))
                {
                    disposeType = DisposeType.Async;
                }
                else if (candidate.ImplementationType.AllInterfaces.Any(symbol =>
                             SymbolEqualityComparer.Default.Equals(symbol, disposableSymbol)))
                {
                    disposeType = DisposeType.Sync;
                }
                return constructor != null
                    ? new InjectableService(
                        candidate.InterfaceType, candidate.ImplementationType,
                        constructor.Parameters.Select(parameter => parameter.Type).Cast<INamedTypeSymbol>()
                            .ToImmutableArray(), disposeType)
                    : null;
            })
            .Where(service => service is not null)
            .Cast<InjectableService>()
            .ToImmutableArray();
    }

    private ImmutableArray<InjectionCandidate> GetInjectionCandidates(
        INamedTypeSymbol type, ImmutableArray<INamedTypeSymbol> provideAttributes)
    {
        return type.GetAttributes()
            .Where(attribute => provideAttributes.Any(provideAttribute =>
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, provideAttribute)))
            .Select<AttributeData, InjectionCandidate?>(
                provideAttribute =>
                {
                    var interfaceType =
                        provideAttribute.ConstructorArguments.Length > 0 &&
                        provideAttribute.ConstructorArguments[0].Value is INamedTypeSymbol interfaceArg
                            ? interfaceArg
                            : null;
                    var implementationType =
                        provideAttribute.ConstructorArguments.Length > 1 &&
                        provideAttribute.ConstructorArguments[1].Value is INamedTypeSymbol implementationArg
                            ? implementationArg
                            : interfaceType;
                    return interfaceType != null && implementationType != null
                        ? new InjectionCandidate(interfaceType, implementationType)
                        : null;
                })
            .Where(candidate => candidate is not null)
            .Cast<InjectionCandidate>()
            .ToImmutableArray();
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
                                attributeDisplayName == AttributeNames.ServiceCollectionBuilder)
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

    private ImmutableArray<AttributedSymbol> ResolveTypes(
        Compilation compilation,
        ImmutableArray<TypeDeclarationSyntax> typeDeclarationSyntaxes,
        ImmutableArray<INamedTypeSymbol> attributeTypeSymbols)
    {
        return typeDeclarationSyntaxes.Select<TypeDeclarationSyntax, AttributedSymbol?>(type =>
            {
                var model = compilation.GetSemanticModel(type.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(type) as INamedTypeSymbol;
                if (symbol == null)
                    return null;
                var attributes = symbol.GetAttributes();
                var attributeData = attributes.FirstOrDefault(attribute => attributeTypeSymbols.Any(
                    attributeTypeSymbol =>
                        SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeTypeSymbol)));
                if (attributeData == null)
                    return null;
                return new AttributedSymbol(symbol, attributeData);
            })
            .Where(symbol => symbol is not null)
            .Cast<AttributedSymbol>()
            .ToImmutableArray();
    }

    private static class AttributeNames
    {
        public const string ProvideTransient = "Fuji.ProvideTransientAttribute";
        public const string ServiceCollectionBuilder = "Fuji.ServiceCollectionBuilderAttribute";
        public const string ServiceProvider = "Fuji.ServiceProviderAttribute";
    }
}