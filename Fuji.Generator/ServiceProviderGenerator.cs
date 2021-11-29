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

        var serviceProviderAttributeType = compilation.GetRequiredTypeByMetadataName(AttributeNames.ServiceProvider);
        var provideTransientAttribute = compilation.GetRequiredTypeByMetadataName(AttributeNames.ProvideTransient);
        var provideAttributes = ImmutableArray.Create(provideTransientAttribute);

        var distinctTypes = typeDeclarationSyntaxes
            .Where(typeSyntax => typeSyntax is not null)
            .Cast<TypeDeclarationSyntax>()
            .Distinct()
            .ToImmutableArray();

        foreach (var serviceProviderType in distinctTypes)
        {
            var model = compilation.GetSemanticModel(serviceProviderType.SyntaxTree);
            var serviceProviderSymbol = model.GetDeclaredSymbol(serviceProviderType) as INamedTypeSymbol;
            if (serviceProviderSymbol == null)
                continue;
            var attributes = serviceProviderSymbol.GetAttributes();
            var serviceProviderAttribute = attributes.FirstOrDefault(attribute =>
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, serviceProviderAttributeType));
            var injectionCandidates =
                GetInjectionCandidates(serviceProviderSymbol, provideAttributes);
            var injectableServices = GetInjectableServices(injectionCandidates);
            var definition = new ServiceProviderDefinition(serviceProviderSymbol, injectableServices,
                serviceProviderAttribute?.NamedArguments.Where(arg => arg.Key == "DebugOutputPath")
                    .Select(arg => arg.Value.Value).FirstOrDefault() as string);

            var fileContent = new SourceCodeGenerator(definition).GenerateServiceProvider();
            var fileName = $"{definition.ServiceProviderType.ToDisplayString()}.generated.cs";
            if (!string.IsNullOrWhiteSpace(definition.DebugOutputPath))
            {
                if (!Directory.Exists(definition.DebugOutputPath))
                    Directory.CreateDirectory(definition.DebugOutputPath);
                File.WriteAllText(Path.Combine(definition.DebugOutputPath, fileName), fileContent);
            }
            sourceProductionContext.AddSource(fileName, fileContent);
        }
    }

    private ImmutableArray<InjectableService> GetInjectableServices(
        ImmutableArray<InjectionCandidate> injectionCandidates)
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

                return constructor != null
                    ? new InjectableService(candidate.InterfaceType, candidate.ImplementationType,
                        constructor.Parameters.Select(parameter => parameter.Type).Cast<INamedTypeSymbol>()
                            .ToImmutableArray())
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
                            if (attributeTypeSymbol != null &&
                                attributeTypeSymbol.ToDisplayString() == AttributeNames.ServiceProvider)
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

    private static class AttributeNames
    {
        public const string ProvideTransient = "Fuji.ProvideTransientAttribute";
        public const string ServiceProvider = "Fuji.ServiceProviderAttribute";
    }
}