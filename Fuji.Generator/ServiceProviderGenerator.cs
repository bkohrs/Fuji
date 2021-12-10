﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fuji;

[Generator]
public class ServiceProviderGenerator : IIncrementalGenerator
{
    private static InjectionCandidate? CreateSelfDescribedInjectionCandidate(AttributedSymbol type, ServiceLifetime serviceLifetime)
    {
        var interfaceType =
            type.Attribute.ConstructorArguments.Length == 1 &&
            type.Attribute.ConstructorArguments[0].Value is INamedTypeSymbol interfaceArg
                ? interfaceArg
                : type.Symbol;
        return new InjectionCandidate(interfaceType, type.Symbol, serviceLifetime);
    }

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
        var provideSingletonAttribute = compilation.GetRequiredTypeByMetadataName(AttributeNames.ProvideSingleton);
        var provideScopedAttribute = compilation.GetRequiredTypeByMetadataName(AttributeNames.ProvideScoped);
        var transientServiceAttribute = compilation.GetRequiredTypeByMetadataName(AttributeNames.TransientService);
        var singletonServiceAttribute = compilation.GetRequiredTypeByMetadataName(AttributeNames.SingletonService);
        var scopedServiceAttribute = compilation.GetRequiredTypeByMetadataName(AttributeNames.ScopedService);
        var provideServiceAttribute = compilation.GetRequiredTypeByMetadataName(AttributeNames.ProvideService);
        var provideAttributes = ImmutableArray.Create(
            (provideTransientAttribute, ServiceLifetime.Transient),
            (provideSingletonAttribute, ServiceLifetime.Singleton),
            (provideScopedAttribute, ServiceLifetime.Scoped));
        var distinctTypes = typeDeclarationSyntaxes
            .Where(typeSyntax => typeSyntax is not null)
            .Cast<TypeDeclarationSyntax>()
            .Distinct()
            .ToImmutableArray();

        var attributeTypeSymbols = ImmutableArray.Create(
            serviceProviderAttributeType,
            serviceCollectionBuilderAttributeType,
            transientServiceAttribute,
            singletonServiceAttribute,
            scopedServiceAttribute);
        var partitionedTypes = ResolveTypes(compilation, distinctTypes, attributeTypeSymbols)
            .ToLookup(type => type.Attribute.AttributeClass, SymbolEqualityComparer.Default);

        var transientSelfDescribedServices = partitionedTypes[transientServiceAttribute]
            .Select(type => CreateSelfDescribedInjectionCandidate(type, ServiceLifetime.Transient));
        var singletonSelfDescribedServices = partitionedTypes[singletonServiceAttribute]
            .Select(type => CreateSelfDescribedInjectionCandidate(type, ServiceLifetime.Singleton));
        var scopedSelfDescribedServices = partitionedTypes[scopedServiceAttribute]
            .Select(type => CreateSelfDescribedInjectionCandidate(type, ServiceLifetime.Scoped));
        var selfDescribedServices = transientSelfDescribedServices
            .Concat(singletonSelfDescribedServices)
            .Concat(scopedSelfDescribedServices)
            .Where(candidate => candidate is not null)
            .Cast<InjectionCandidate>()
            .ToImmutableArray();

        foreach (var provider in partitionedTypes[serviceProviderAttributeType])
        {
            GenerateCode(sourceProductionContext, provider, provideAttributes,
                asyncDisposableSymbol, disposableSymbol, selfDescribedServices,provideServiceAttribute,
                definition => new SourceCodeGenerator(definition).GenerateServiceProvider());
        }
        foreach (var provider in partitionedTypes[serviceCollectionBuilderAttributeType])
        {
            GenerateCode(sourceProductionContext, provider, provideAttributes,
                asyncDisposableSymbol, disposableSymbol, selfDescribedServices,provideServiceAttribute,
                definition => new SourceCodeGenerator(definition).GenerateServiceCollectionBuilder());
        }
    }

    private void GenerateCode(SourceProductionContext sourceProductionContext, AttributedSymbol provider,
        ImmutableArray<(INamedTypeSymbol Symbol, ServiceLifetime Lifetime)> provideAttributes,
        INamedTypeSymbol asyncDisposableSymbol, INamedTypeSymbol disposableSymbol,
        ImmutableArray<InjectionCandidate> selfDescribedServices,
        INamedTypeSymbol provideServiceAttribute,
        Func<ServiceProviderDefinition, string> generateContent)
    {
        var injectionCandidates =
        GetInjectionCandidates(provider.Symbol, provideAttributes,
            GetSelfProvidedServices(provider, selfDescribedServices, provideServiceAttribute));
        var injectableServices = GetInjectableServices(injectionCandidates, asyncDisposableSymbol, disposableSymbol,
            selfDescribedServices);
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
        INamedTypeSymbol disposableSymbol, IEnumerable<InjectionCandidate> selfDescribedServices)
    {
        var identifiedServices = new Dictionary<ISymbol, InjectableService>(SymbolEqualityComparer.Default);
        var validServices = injectionCandidates.Concat(selfDescribedServices)
            .ToImmutableDictionary<InjectionCandidate, ISymbol>(candidate => candidate.InterfaceType, SymbolEqualityComparer.Default);
        var services = new Queue<InjectionCandidate>(injectionCandidates);
        while (services.Count > 0)
        {
            var service = services.Dequeue();
            if (identifiedServices.ContainsKey(service.InterfaceType))
                continue;
            var constructor = service.ImplementationType.Constructors
                .OrderByDescending(ctor => ctor.Parameters.Length)
                .FirstOrDefault(ctor =>
                    ctor.Parameters.All(parameter => validServices.ContainsKey(parameter.Type)));
            if (constructor == null)
                continue;
            var disposeType = DisposeType.None;
            if (service.ImplementationType.AllInterfaces.Any(symbol =>
                    SymbolEqualityComparer.Default.Equals(symbol, asyncDisposableSymbol)))
            {
                disposeType = DisposeType.Async;
            }
            else if (service.ImplementationType.AllInterfaces.Any(symbol =>
                         SymbolEqualityComparer.Default.Equals(symbol, disposableSymbol)))
            {
                disposeType = DisposeType.Sync;
            }
            var constructorArguments = constructor.Parameters
                .Select(parameter => parameter.Type)
                .Cast<INamedTypeSymbol>()
                .ToImmutableArray();
            identifiedServices[service.InterfaceType] = new InjectableService(service.InterfaceType,
                service.ImplementationType, service.Lifetime, constructorArguments, disposeType);
            foreach (var argument in constructorArguments)
            {
                if (identifiedServices.ContainsKey(argument))
                    continue;
                services.Enqueue(validServices[argument]);
            }
        }
        return identifiedServices.Values.ToImmutableArray();
    }

    private ImmutableArray<InjectionCandidate> GetInjectionCandidates(
        INamedTypeSymbol type, ImmutableArray<(INamedTypeSymbol Symbol, ServiceLifetime Lifetime)> provideAttributes,
        ImmutableArray<InjectionCandidate> selfProvidedServices)
    {
        return type.GetAttributes()
            .Select<AttributeData, InjectionCandidate?>(
                provideAttribute =>
                {
                    var lifetime = provideAttributes
                        .Where(attr =>
                            SymbolEqualityComparer.Default.Equals(provideAttribute.AttributeClass, attr.Symbol))
                        .Select(attr => (ServiceLifetime?)attr.Lifetime)
                        .FirstOrDefault();
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
                    return lifetime != null && interfaceType != null && implementationType != null
                        ? new InjectionCandidate(interfaceType, implementationType, lifetime.Value)
                        : null;
                })
            .Where(candidate => candidate is not null)
            .Cast<InjectionCandidate>()
            .Concat(selfProvidedServices)
            .ToImmutableArray();
    }

    private ImmutableArray<InjectionCandidate> GetSelfProvidedServices(AttributedSymbol provider,
        ImmutableArray<InjectionCandidate> selfDescribedServices, INamedTypeSymbol provideServiceAttribute)
    {
        return selfDescribedServices.Where(service => service.ImplementationType.GetAttributes().Any(attribute =>
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, provideServiceAttribute))
                return false;
            if (attribute.ConstructorArguments.Length == 1 &&
                attribute.ConstructorArguments[0].Value is INamedTypeSymbol namedTypeSymbol)
            {
                return SymbolEqualityComparer.Default.Equals(namedTypeSymbol, provider.Symbol);
            }
            return false;
        })).ToImmutableArray();
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
        public const string ProvideScoped = "Fuji.ProvideScopedAttribute";
        public const string ProvideService = "Fuji.ProvideServiceAttribute";
        public const string ProvideSingleton = "Fuji.ProvideSingletonAttribute";
        public const string ProvideTransient = "Fuji.ProvideTransientAttribute";
        public const string ScopedService = "Fuji.ScopedServiceAttribute";
        public const string ServiceCollectionBuilder = "Fuji.ServiceCollectionBuilderAttribute";
        public const string ServiceProvider = "Fuji.ServiceProviderAttribute";
        public const string SingletonService = "Fuji.SingletonServiceAttribute";
        public const string TransientService = "Fuji.TransientServiceAttribute";
    }
}