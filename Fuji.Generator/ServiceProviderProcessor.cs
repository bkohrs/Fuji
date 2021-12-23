using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fuji;

public class ServiceProviderProcessor
{
    private readonly INamedTypeSymbol _asyncDisposableSymbol;
    private readonly ImmutableArray<INamedTypeSymbol> _attributeTypeSymbols;
    private readonly Compilation _compilation;
    private readonly INamedTypeSymbol _disposableSymbol;
    private readonly INamedTypeSymbol _enumerableSymbol;
    private readonly ImmutableArray<AttributedSymbol> _libraryCandidates;
    private readonly ImmutableArray<(INamedTypeSymbol Symbol, ServiceLifetime Lifetime)> _provideAttributes;
    private readonly INamedTypeSymbol _providedByCollectionAttribute;
    private readonly INamedTypeSymbol _provideServiceAttribute;
    private readonly INamedTypeSymbol _scopedServiceAttribute;
    private readonly INamedTypeSymbol _serviceCollectionBuilderAttributeType;
    private readonly INamedTypeSymbol _serviceProviderAttributeType;
    private readonly INamedTypeSymbol _singletonServiceAttribute;
    private readonly SourceProductionContext _sourceProductionContext;
    private readonly INamedTypeSymbol _transientServiceAttribute;

    public ServiceProviderProcessor(Compilation compilation, SourceProductionContext sourceProductionContext)
    {
        _compilation = compilation;
        _sourceProductionContext = sourceProductionContext;
        _asyncDisposableSymbol = compilation.GetRequiredTypeByMetadataName("System.IAsyncDisposable");
        _disposableSymbol = compilation.GetRequiredTypeByMetadataName("System.IDisposable");
        _enumerableSymbol = compilation.GetRequiredTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        _serviceProviderAttributeType = compilation.GetRequiredTypeByMetadataName(AttributeNames.ServiceProvider);
        _serviceCollectionBuilderAttributeType = compilation.GetRequiredTypeByMetadataName(AttributeNames.ServiceCollectionBuilder);
        _transientServiceAttribute = compilation.GetRequiredTypeByMetadataName(AttributeNames.TransientService);
        _singletonServiceAttribute = compilation.GetRequiredTypeByMetadataName(AttributeNames.SingletonService);
        _scopedServiceAttribute = compilation.GetRequiredTypeByMetadataName(AttributeNames.ScopedService);
        _provideServiceAttribute = compilation.GetRequiredTypeByMetadataName(AttributeNames.ProvideService);
        _providedByCollectionAttribute = compilation.GetRequiredTypeByMetadataName("Fuji.ProvidedByCollectionAttribute");
        _provideAttributes = ImmutableArray.Create(
            (compilation.GetRequiredTypeByMetadataName(AttributeNames.ProvideTransient), ServiceLifetime.Transient),
            (compilation.GetRequiredTypeByMetadataName(AttributeNames.ProvideSingleton), ServiceLifetime.Singleton),
            (compilation.GetRequiredTypeByMetadataName(AttributeNames.ProvideScoped), ServiceLifetime.Scoped));
        _attributeTypeSymbols = ImmutableArray.Create(
            _serviceProviderAttributeType,
            _serviceCollectionBuilderAttributeType,
            _transientServiceAttribute,
            _singletonServiceAttribute,
            _scopedServiceAttribute);
        _libraryCandidates = GetLibraryCandidates(
            ImmutableArray.Create(_transientServiceAttribute, _singletonServiceAttribute, _scopedServiceAttribute));

    }

    private static InjectionCandidate? CreateSelfDescribedInjectionCandidate(AttributedSymbol type,
        ServiceLifetime serviceLifetime)
    {
        var interfaceType =
            type.Attribute.ConstructorArguments.Length == 1 &&
            type.Attribute.ConstructorArguments[0].Value is INamedTypeSymbol interfaceArg
                ? interfaceArg
                : type.Symbol;
        var priority = type.Attribute.NamedArguments.Where(arg => arg.Key == "Priority")
            .Select(arg => Convert.ToInt32(arg.Value.Value)).FirstOrDefault();
        return new InjectionCandidate(interfaceType, type.Symbol, serviceLifetime, null, priority);
    }

    private void GenerateCode(AttributedSymbol provider,
        ImmutableArray<InjectionCandidate> selfDescribedServices,
        INamedTypeSymbol? providedByCollectionAttribute,
        Func<ServiceProviderDefinition, DiagnosticReporter, string> generateContent)
    {
        var injectionCandidates =
            GetInjectionCandidates(provider.Symbol,
                GetSelfProvidedServices(provider, selfDescribedServices));
        var providedByCollection = providedByCollectionAttribute != null
            ? GetProvidedByCollectionServices(provider, providedByCollectionAttribute)
            : Enumerable.Empty<INamedTypeSymbol>();
        var includeAllServices =
            Convert.ToBoolean(provider.Attribute.NamedArguments.Where(arg => arg.Key == "IncludeAllServices")
                .Select(arg => arg.Value.Value).FirstOrDefault());
        var diagnosticReporter = new DiagnosticReporter(_sourceProductionContext);
        var injectableServices = GetInjectableServices(diagnosticReporter, provider.Symbol, injectionCandidates, selfDescribedServices, providedByCollection,
            includeAllServices);

        if (diagnosticReporter.HasError)
            return;

        var debugOutputPath =
            provider.Attribute.NamedArguments.Where(arg => arg.Key == "DebugOutputPath")
                .Select(arg => arg.Value.Value).FirstOrDefault() as string;
        var definition = new ServiceProviderDefinition(provider.Symbol, injectableServices,
            debugOutputPath);

        var fileContent = generateContent(definition, diagnosticReporter);
        if (string.IsNullOrWhiteSpace(fileContent))
            return;
        var fileName = $"{definition.ServiceProviderType.ToDisplayString()}.generated.cs";
        if (!string.IsNullOrWhiteSpace(definition.DebugOutputPath))
        {
            if (!Directory.Exists(definition.DebugOutputPath))
                Directory.CreateDirectory(definition.DebugOutputPath);
            File.WriteAllText(Path.Combine(definition.DebugOutputPath, fileName), fileContent);
        }
        _sourceProductionContext.AddSource(fileName, fileContent);
    }

    private ImmutableArray<INamedTypeSymbol> GetConstructorArguments(DiagnosticReporter diagnosticReporter,
        ITypeSymbol providerType, InjectionCandidate service, Func<ITypeSymbol, bool> isValidService)
    {
        if (service.CustomFactory != null)
            return ImmutableArray<INamedTypeSymbol>.Empty;
        var constructor = service.ImplementationType.Constructors
            .OrderByDescending(ctor => ctor.Parameters.Length)
            .FirstOrDefault(ctor =>
                ctor.Parameters.All(parameter => isValidService(parameter.Type)));
        if (constructor == null)
        {
            var targetConstructor = service.ImplementationType.Constructors
                .OrderByDescending(ctor => ctor.Parameters.Length)
                .First();
            var missingTypes = targetConstructor.Parameters.Where(parameter => !isValidService(parameter.Type))
                .Select(parameter => parameter.Type).ToImmutableArray();
            diagnosticReporter.ReportMissingServices(providerType, service.ImplementationType, missingTypes,
                targetConstructor.Locations.First());
            return ImmutableArray<INamedTypeSymbol>.Empty;
        }
        return constructor.Parameters
            .Select(parameter => parameter.Type)
            .Cast<INamedTypeSymbol>()
            .ToImmutableArray();
    }

    private ImmutableArray<InjectableService> GetInjectableServices(
        DiagnosticReporter diagnosticReporter, ITypeSymbol providerType,
        ImmutableArray<InjectionCandidate> injectionCandidates,
        ImmutableArray<InjectionCandidate> selfDescribedServices,
        IEnumerable<INamedTypeSymbol> providedByCollection, bool includeAllServices)
    {
        var identifiedServices = new Dictionary<ISymbol, InjectableService>(SymbolEqualityComparer.Default);
        var providedByCollectionHashSet = providedByCollection.ToImmutableHashSet(SymbolEqualityComparer.Default);

        bool ServiceHasBeenProcessed(INamedTypeSymbol symbol)
        {
            return providedByCollectionHashSet.Contains(symbol) || identifiedServices.ContainsKey(symbol);
        }

        var validServices = injectionCandidates.Concat(selfDescribedServices)
            .GroupBy(candidate => candidate.InterfaceType, SymbolEqualityComparer.Default)
            .Select(group => group.Key)
            .ToImmutableHashSet(SymbolEqualityComparer.Default);
        var serviceLookup = injectionCandidates.Concat(selfDescribedServices)
            .ToLookup(candidate => candidate.InterfaceType, SymbolEqualityComparer.Default);
        var services = new Queue<InjectionCandidate>(injectionCandidates);
        if (includeAllServices)
        {
            foreach (var service in selfDescribedServices)
                services.Enqueue(service);
        }
        while (services.Count > 0)
        {
            var service = services.Dequeue();
            if (ServiceHasBeenProcessed(service.InterfaceType))
                continue;
            var disposeType = DisposeType.None;
            if (service.ImplementationType.AllInterfaces.Any(symbol =>
                    SymbolEqualityComparer.Default.Equals(symbol, _asyncDisposableSymbol)))
            {
                disposeType = DisposeType.Async;
            }
            else if (service.ImplementationType.AllInterfaces.Any(symbol =>
                         SymbolEqualityComparer.Default.Equals(symbol, _disposableSymbol)))
            {
                disposeType = DisposeType.Sync;
            }
            var constructorArguments = GetConstructorArguments(diagnosticReporter, providerType, service,
                type =>
                {
                    var resolvedType =
                        type is INamedTypeSymbol { IsGenericType: true } namedType &&
                        SymbolEqualityComparer.Default.Equals(_enumerableSymbol, type.OriginalDefinition)
                            ? namedType.TypeArguments[0]
                            : type;
                    return providedByCollectionHashSet.Contains(resolvedType) || validServices.Contains(resolvedType);
                });
            identifiedServices[service.ImplementationType] = new InjectableService(service.InterfaceType,
                service.ImplementationType, service.Lifetime, constructorArguments, disposeType, service.CustomFactory,
                service.Priority);
            foreach (var argument in constructorArguments)
            {
                if (ServiceHasBeenProcessed(argument))
                    continue;
                foreach (var argService in serviceLookup[argument])
                    services.Enqueue(argService);
            }
        }
        return identifiedServices.Values.ToImmutableArray();
    }

    private ImmutableArray<InjectionCandidate> GetInjectionCandidates(
        INamedTypeSymbol type, ImmutableArray<InjectionCandidate> selfProvidedServices)
    {
        return type.GetAttributes()
            .Select<AttributeData, InjectionCandidate?>(
                provideAttribute =>
                {
                    var lifetime = _provideAttributes
                        .Where(attr =>
                            SymbolEqualityComparer.Default.Equals(provideAttribute.AttributeClass, attr.Symbol))
                        .Select(attr => (ServiceLifetime?)attr.Lifetime)
                        .FirstOrDefault();

                    var customFactory = provideAttribute.NamedArguments.Where(arg => arg.Key == "Factory")
                        .Select(arg => arg.Value.Value as string).FirstOrDefault();
                    var priority = provideAttribute.NamedArguments.Where(arg => arg.Key == "Priority")
                        .Select(arg => Convert.ToInt32(arg.Value.Value)).FirstOrDefault();
                    var customFactoryMethod = !string.IsNullOrWhiteSpace(customFactory)
                        ? type.GetMembers(customFactory!).FirstOrDefault() as IMethodSymbol
                        : null;
                    var interfaceType =
                        provideAttribute.ConstructorArguments.Length > 0 &&
                        provideAttribute.ConstructorArguments[0].Value is INamedTypeSymbol interfaceArg
                            ? interfaceArg
                            : null;
                    var implementationType =
                        provideAttribute.ConstructorArguments.Length > 1 &&
                        !provideAttribute.ConstructorArguments[1].IsNull &&
                        provideAttribute.ConstructorArguments[1].Value is INamedTypeSymbol implementationArg
                            ? implementationArg
                            : interfaceType;
                    return lifetime != null && interfaceType != null && implementationType != null
                        ? new InjectionCandidate(interfaceType, implementationType, lifetime.Value, customFactoryMethod,
                            priority)
                        : null;
                })
            .Where(candidate => candidate is not null)
            .Cast<InjectionCandidate>()
            .Concat(selfProvidedServices)
            .ToImmutableArray();
    }

    private ImmutableArray<AttributedSymbol> GetLibraryCandidates(
        ImmutableArray<INamedTypeSymbol> attributeTypeSymbols)
    {
        return _compilation.SourceModule.ReferencedAssemblySymbols
            .SelectMany(assemblySymbol => GetSymbols(assemblySymbol.GlobalNamespace, attributeTypeSymbols))
            .ToImmutableArray();
    }

    private IEnumerable<INamedTypeSymbol> GetProvidedByCollectionServices(AttributedSymbol provider,
        INamedTypeSymbol providedByCollectionAttribute)
    {
        return provider.Symbol.GetAttributes()
            .Select<AttributeData, INamedTypeSymbol?>(attribute =>
            {
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, providedByCollectionAttribute))
                    return null;
                if (attribute.ConstructorArguments.Length == 1 &&
                    attribute.ConstructorArguments[0].Value is INamedTypeSymbol namedTypeSymbol)
                {
                    return namedTypeSymbol;
                }
                return null;
            })
            .Where(namedTypeSymbol => namedTypeSymbol is not null)
            .Select(symbol => symbol!);
    }

    private ImmutableArray<InjectionCandidate> GetSelfProvidedServices(AttributedSymbol provider,
        ImmutableArray<InjectionCandidate> selfDescribedServices)
    {
        return selfDescribedServices.Where(service => service.ImplementationType.GetAttributes().Any(attribute =>
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _provideServiceAttribute))
                return false;
            if (attribute.ConstructorArguments.Length == 1 &&
                attribute.ConstructorArguments[0].Value is INamedTypeSymbol namedTypeSymbol)
            {
                return SymbolEqualityComparer.Default.Equals(namedTypeSymbol, provider.Symbol);
            }
            return false;
        })).ToImmutableArray();
    }

    private IEnumerable<AttributedSymbol> GetSymbols(INamespaceSymbol namespaceSymbol,
        ImmutableArray<INamedTypeSymbol> attributeTypeSymbols)
    {
        foreach (var symbol in namespaceSymbol.GetTypeMembers())
        {
            var attributes = symbol.GetAttributes();
            var attributeData = attributes.FirstOrDefault(attribute => attributeTypeSymbols.Any(
                attributeTypeSymbol =>
                    SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeTypeSymbol)));
            if (attributeData != null)
                yield return new AttributedSymbol(symbol, attributeData);
        }
        foreach (var subNamespaceSymbol in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var symbol in GetSymbols(subNamespaceSymbol, attributeTypeSymbols))
                yield return symbol;
        }
    }

    public void Process(ImmutableArray<TypeDeclarationSyntax> serviceProviderTypes)
    {
        var partitionedTypes = ResolveTypes(serviceProviderTypes)
            .Concat(_libraryCandidates)
            .ToLookup(type => type.Attribute.AttributeClass, SymbolEqualityComparer.Default);

        var transientSelfDescribedServices = partitionedTypes[_transientServiceAttribute]
            .Select(type => CreateSelfDescribedInjectionCandidate(type, ServiceLifetime.Transient));
        var singletonSelfDescribedServices = partitionedTypes[_singletonServiceAttribute]
            .Select(type => CreateSelfDescribedInjectionCandidate(type, ServiceLifetime.Singleton));
        var scopedSelfDescribedServices = partitionedTypes[_scopedServiceAttribute]
            .Select(type => CreateSelfDescribedInjectionCandidate(type, ServiceLifetime.Scoped));
        var selfDescribedServices = transientSelfDescribedServices
            .Concat(singletonSelfDescribedServices)
            .Concat(scopedSelfDescribedServices)
            .Where(candidate => candidate is not null)
            .Cast<InjectionCandidate>()
            .ToImmutableArray();

        foreach (var provider in partitionedTypes[_serviceProviderAttributeType])
        {
            GenerateCode(provider, selfDescribedServices, null,
                (definition, diagnosticReporter) =>
                    new SourceCodeGenerator(definition, diagnosticReporter).GenerateServiceProvider());
        }
        foreach (var provider in partitionedTypes[_serviceCollectionBuilderAttributeType])
        {
            GenerateCode(provider, selfDescribedServices,
                _providedByCollectionAttribute,
                (definition, diagnosticReporter) =>
                    new SourceCodeGenerator(definition, diagnosticReporter).GenerateServiceCollectionBuilder());
        }
    }

    private ImmutableArray<AttributedSymbol> ResolveTypes(
        ImmutableArray<TypeDeclarationSyntax> typeDeclarationSyntaxes)
    {
        return typeDeclarationSyntaxes.Select<TypeDeclarationSyntax, AttributedSymbol?>(type =>
            {
                var model = _compilation.GetSemanticModel(type.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(type) as INamedTypeSymbol;
                if (symbol == null)
                    return null;
                var attributes = symbol.GetAttributes();
                var attributeData = attributes.FirstOrDefault(attribute => _attributeTypeSymbols.Any(
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
}