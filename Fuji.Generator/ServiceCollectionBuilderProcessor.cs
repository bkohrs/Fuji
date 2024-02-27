using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fuji;

public class ServiceCollectionBuilderProcessor
{
    private readonly ImmutableArray<INamedTypeSymbol> _attributeTypeSymbols;
    private readonly Compilation _compilation;
    private readonly INamedTypeSymbol _enumerableSymbol;
    private readonly INamedTypeSymbol? _fromKeyedServicesAttribute;
    private readonly ImmutableArray<INamedTypeSymbol> _libraryTypes;
    private readonly INamedTypeSymbol _obsoleteSymbol;
    private readonly ImmutableArray<(INamedTypeSymbol Symbol, ServiceLifetime Lifetime)> _provideAttributes;
    private readonly INamedTypeSymbol _providedByCollectionAttribute;
    private readonly INamedTypeSymbol _provideServiceAttribute;
    private readonly INamedTypeSymbol _scopedServiceAttribute;
    private readonly INamedTypeSymbol _serviceCollectionBuilderAttributeType;
    private readonly INamedTypeSymbol _singletonServiceAttribute;
    private readonly SourceProductionContext _sourceProductionContext;
    private readonly INamedTypeSymbol _transientServiceAttribute;

    public ServiceCollectionBuilderProcessor(Compilation compilation, SourceProductionContext sourceProductionContext)
    {
        _compilation = compilation;
        _sourceProductionContext = sourceProductionContext;
        _enumerableSymbol = compilation.GetRequiredTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        _fromKeyedServicesAttribute = compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute");
        _obsoleteSymbol = compilation.GetRequiredTypeByMetadataName("System.ObsoleteAttribute");
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
            _serviceCollectionBuilderAttributeType,
            _transientServiceAttribute,
            _singletonServiceAttribute,
            _scopedServiceAttribute);
        _libraryTypes = GetLibraryTypes();

    }

    private static InjectionCandidate CreateSelfDescribedInjectionCandidate(AttributedSymbol type,
        ServiceLifetime serviceLifetime)
    {
        var interfaceType =
            type.Attribute.ConstructorArguments.Length == 1 &&
            type.Attribute.ConstructorArguments[0].Value is INamedTypeSymbol interfaceArg
                ? interfaceArg
                : type.Symbol;
        var priority = type.Attribute.NamedArguments.Where(arg => arg.Key == "Priority")
            .Select(arg => Convert.ToInt32(arg.Value.Value)).FirstOrDefault();
        var key = type.Attribute.NamedArguments.Where(arg => arg.Key == "Key")
            .Select(arg => Convert.ToString(arg.Value.Value)).FirstOrDefault();
        return new InjectionCandidate(interfaceType, type.Symbol, serviceLifetime, key, null, priority);
    }

    private void GenerateCode(AttributedSymbol provider,
        ImmutableArray<InjectionCandidate> selfDescribedServices,
        INamedTypeSymbol? providedByCollectionAttribute,
        ImmutableArray<INamedTypeSymbol> allTypes,
        Func<ServiceCollectionBuilderDefinition, string> generateContent)
    {
        var injectionCandidates =
            GetInjectionCandidates(provider.Symbol,
                GetSelfProvidedServices(provider, selfDescribedServices));
        var providedByCollection = providedByCollectionAttribute != null
            ? GetProvidedByCollectionServices(provider, providedByCollectionAttribute)
            : Enumerable.Empty<(ISymbol, string?)>();
        var includeAllServices =
            Convert.ToBoolean(provider.Attribute.NamedArguments.Where(arg => arg.Key == "IncludeAllServices")
                .Select(arg => arg.Value.Value).FirstOrDefault());
        ImmutableArray<INamedTypeSymbol> serviceRoots = ImmutableArray<INamedTypeSymbol>.Empty;
        var includeInterfaceImplementors =
            provider.Attribute.NamedArguments.Where(arg => arg.Key == "IncludeInterfaceImplementors")
                .Select(arg => arg.Value.Value as INamedTypeSymbol).FirstOrDefault();
        if (includeInterfaceImplementors != null)
        {
            serviceRoots = allTypes.Where(type =>
                    !type.IsAbstract && type.AllInterfaces.Contains(includeInterfaceImplementors, SymbolEqualityComparer.Default))
                .ToImmutableArray();
        }
        var diagnosticReporter = new DiagnosticReporter(_sourceProductionContext);
        var injectableServices = GetInjectableServices(diagnosticReporter, provider.Symbol, injectionCandidates, selfDescribedServices, providedByCollection,
            includeAllServices, serviceRoots);

        if (diagnosticReporter.HasError)
            return;

        var definition = new ServiceCollectionBuilderDefinition(provider.Symbol, injectableServices);

        var fileContent = generateContent(definition);
        if (string.IsNullOrWhiteSpace(fileContent))
            return;
        var fileName = $"{definition.ServiceCollectionBuilderType.ToDisplayString()}.generated.cs";
        _sourceProductionContext.AddSource(fileName, fileContent);
    }

    private ImmutableArray<(INamedTypeSymbol Symbol, string? Key)> GetConstructorArguments(DiagnosticReporter diagnosticReporter,
        ITypeSymbol providerType, INamedTypeSymbol service, IMethodSymbol? customFactory, Func<(ITypeSymbol Type, string? Key), bool> isValidService)
    {
        if (customFactory != null)
            return ImmutableArray<(INamedTypeSymbol Symbol, string? Key)>.Empty;
        var constructor = service.Constructors
            .OrderByDescending(ctor => ctor.Parameters.Length)
            .FirstOrDefault(ctor =>
                ctor.Parameters.All(parameter => isValidService((parameter.Type, GetParameterKey(parameter)))));
        if (constructor == null)
        {
            var targetConstructor = service.Constructors
                .OrderByDescending(ctor => ctor.Parameters.Length)
                .First();
            var missingTypes = targetConstructor.Parameters.Where(parameter => !isValidService((parameter.Type, GetParameterKey(parameter))))
                .Select(parameter => (parameter.Type, GetParameterKey(parameter))).ToImmutableArray();
            diagnosticReporter.ReportMissingServices(providerType, service, missingTypes,
                targetConstructor.Locations.First());
            return ImmutableArray<(INamedTypeSymbol Symbol, string? Key)>.Empty;
        }
        return constructor.Parameters
            .Select(parameter => ((INamedTypeSymbol)parameter.Type, GetParameterKey(parameter)))
            .ToImmutableArray();
    }

    private ImmutableArray<InjectableService> GetInjectableServices(
        DiagnosticReporter diagnosticReporter, ITypeSymbol providerType,
        ImmutableArray<InjectionCandidate> injectionCandidates,
        ImmutableArray<InjectionCandidate> selfDescribedServices,
        IEnumerable<(ISymbol, string? Key)> providedByCollection, bool includeAllServices,
        ImmutableArray<INamedTypeSymbol> serviceRoots)
    {
        var identifiedServices = new Dictionary<(ISymbol, string?), InjectableService>(KeyedServiceComparer.Instance);
        var providedByCollectionHashSet = providedByCollection.ToImmutableHashSet(KeyedServiceComparer.Instance);

        bool ServiceHasBeenProcessed((INamedTypeSymbol Symbol, string? Key) value)
        {
            return providedByCollectionHashSet.Contains(value) || identifiedServices.ContainsKey(value);
        }

        var validServices = injectionCandidates.Concat(selfDescribedServices)
            .GroupBy(candidate => (candidate.InterfaceType, candidate.Key), KeyedServiceComparer.Instance)
            .Select(group => group.Key)
            .ToImmutableHashSet(KeyedServiceComparer.Instance);
        var serviceLookup = injectionCandidates.Concat(selfDescribedServices)
            .ToLookup(candidate => (candidate.InterfaceType, candidate.Key), KeyedServiceComparer.Instance);
        var services = new Queue<InjectionCandidate>(injectionCandidates);
        if (includeAllServices)
        {
            foreach (var service in selfDescribedServices)
                services.Enqueue(service);
        }

        foreach (var serviceRoot in serviceRoots)
        {
            var constructorArguments =
                GetConstructorArguments(diagnosticReporter, providerType, serviceRoot, null, IsValidService());
            foreach (var arg in constructorArguments)
            {
                foreach (var service in serviceLookup[arg])
                    services.Enqueue(service);
            }
        }

        while (services.Count > 0)
        {
            var service = services.Dequeue();
            if (ServiceHasBeenProcessed((service.InterfaceType, service.Key)) || ServiceHasBeenProcessed((service.ImplementationType, service.Key)))
                continue;
            var constructorArguments = GetConstructorArguments(diagnosticReporter, providerType,
                service.ImplementationType, service.CustomFactory, IsValidService());
            var hasObsoleteAttribute =
                service.InterfaceType.GetAttributes().Any(r =>
                    SymbolEqualityComparer.Default.Equals(_obsoleteSymbol, r.AttributeClass)) ||
                service.ImplementationType.GetAttributes().Any(r =>
                    SymbolEqualityComparer.Default.Equals(_obsoleteSymbol, r.AttributeClass));
            identifiedServices.Add((service.ImplementationType, service.Key),  new InjectableService(service.InterfaceType,
                service.ImplementationType, service.Lifetime, service.CustomFactory,
                service.Key, service.Priority, hasObsoleteAttribute));
            foreach (var argument in constructorArguments)
            {
                if (ServiceHasBeenProcessed(argument))
                    continue;
                foreach (var argService in serviceLookup[argument])
                    services.Enqueue(argService);
            }
        }
        return identifiedServices.Values.ToImmutableArray();

        Func<(ITypeSymbol Type, string? Key), bool> IsValidService()
        {
            return (definition) =>
            {
                var resolvedType =
                    definition.Type is INamedTypeSymbol { IsGenericType: true } namedType &&
                    SymbolEqualityComparer.Default.Equals(_enumerableSymbol, definition.Type.OriginalDefinition)
                        ? namedType.TypeArguments[0]
                        : definition.Type;
                return providedByCollectionHashSet.Contains((resolvedType, definition.Key)) || validServices.Contains((resolvedType, definition.Key));
            };
        }
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
                    var key = provideAttribute.NamedArguments.Where(arg => arg.Key == "Key")
                        .Select(arg => Convert.ToString(arg.Value.Value)).FirstOrDefault();
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
                        ? new InjectionCandidate(interfaceType, implementationType, lifetime.Value, key, customFactoryMethod,
                            priority)
                        : null;
                })
            .Where(candidate => candidate is not null)
            .Cast<InjectionCandidate>()
            .Concat(selfProvidedServices)
            .ToImmutableArray();
    }

    private ImmutableArray<INamedTypeSymbol> GetLibraryTypes()
    {
        return _compilation.SourceModule.ReferencedAssemblySymbols
            .SelectMany(assemblySymbol => GetSymbols(assemblySymbol.GlobalNamespace))
            .ToImmutableArray();
    }

    private string? GetParameterKey(IParameterSymbol parameter)
    {
        var fromKeyedServices = parameter.GetAttributes().FirstOrDefault(attribute =>
            SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _fromKeyedServicesAttribute));
        return fromKeyedServices is { ConstructorArguments.Length: > 0 } &&
               fromKeyedServices.ConstructorArguments[0] is var typedConstant
            ? Convert.ToString(typedConstant.Value)
            : null;
    }

    private IEnumerable<(ISymbol, string?)> GetProvidedByCollectionServices(AttributedSymbol provider,
        INamedTypeSymbol providedByCollectionAttribute)
    {
        return from namedTypeSymbol in provider.Symbol.GetAttributes()
                .Select<AttributeData, (ISymbol Symbol, string? Key)?>(attribute =>
                {
                    if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, providedByCollectionAttribute))
                        return null;
                    if (attribute.ConstructorArguments.Length == 1 &&
                        attribute.ConstructorArguments[0].Value is INamedTypeSymbol namedTypeSymbol)
                    {
                        var key = attribute.NamedArguments.Where(arg => arg.Key == "Key")
                            .Select(arg => Convert.ToString(arg.Value.Value)).FirstOrDefault();
                        return (namedTypeSymbol, key);
                    }

                    return null;
                })
            where namedTypeSymbol != null
            select namedTypeSymbol.Value;
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

    private IEnumerable<INamedTypeSymbol> GetSymbols(INamespaceSymbol namespaceSymbol)
    {
        foreach (var symbol in namespaceSymbol.GetTypeMembers())
            yield return symbol;
        foreach (var subNamespaceSymbol in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var symbol in GetSymbols(subNamespaceSymbol))
                yield return symbol;
        }
    }

    public void Process(ImmutableArray<TypeDeclarationSyntax> typeSyntaxes)
    {
        var allTypes = ResolveTypes(typeSyntaxes).Concat(_libraryTypes).ToImmutableArray();
        var attributedSymbols = from type in allTypes.Select(symbol =>
            {
                var attributes = symbol.GetAttributes();
                var attributeData = attributes.FirstOrDefault(attribute => _attributeTypeSymbols.Any(
                    attributeTypeSymbol =>
                        SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeTypeSymbol)));
                return (symbol, attributeData);
            })
            where type.attributeData != null
            select new AttributedSymbol(type.symbol, type.attributeData);
        var partitionedTypes =
            attributedSymbols.ToLookup(type => type.Attribute.AttributeClass, SymbolEqualityComparer.Default);

        var transientSelfDescribedServices = partitionedTypes[_transientServiceAttribute]
            .Select(type => CreateSelfDescribedInjectionCandidate(type, ServiceLifetime.Transient));
        var singletonSelfDescribedServices = partitionedTypes[_singletonServiceAttribute]
            .Select(type => CreateSelfDescribedInjectionCandidate(type, ServiceLifetime.Singleton));
        var scopedSelfDescribedServices = partitionedTypes[_scopedServiceAttribute]
            .Select(type => CreateSelfDescribedInjectionCandidate(type, ServiceLifetime.Scoped));
        var selfDescribedServices = transientSelfDescribedServices
            .Concat(singletonSelfDescribedServices)
            .Concat(scopedSelfDescribedServices)
            .ToImmutableArray();

        var providers = partitionedTypes[_serviceCollectionBuilderAttributeType].Where(symbol =>
            SymbolEqualityComparer.Default.Equals(symbol.Symbol.ContainingAssembly, _compilation.Assembly));
        foreach (var provider in providers)
        {
            GenerateCode(provider, selfDescribedServices, _providedByCollectionAttribute, allTypes,
                definition =>
                    new SourceCodeGenerator(definition).GenerateServiceCollectionBuilder());
        }
    }

    private ImmutableArray<INamedTypeSymbol> ResolveTypes(ImmutableArray<TypeDeclarationSyntax> typeDeclarationSyntaxes)
    {
        return (from type in typeDeclarationSyntaxes.Select<TypeDeclarationSyntax, INamedTypeSymbol?>(type =>
                {
                    var model = _compilation.GetSemanticModel(type.SyntaxTree);
                    var symbol = model.GetDeclaredSymbol(type) as INamedTypeSymbol;
                    return symbol;
                })
                where type != null
                select type)
            .ToImmutableArray();
    }

    private class KeyedServiceComparer : IEqualityComparer<(ISymbol, string?)>
    {
        public static KeyedServiceComparer Instance { get; } = new KeyedServiceComparer();
        public bool Equals((ISymbol, string?) x, (ISymbol, string?) y)
        {
            return SymbolEqualityComparer.Default.Equals(x.Item1, y.Item1) && x.Item2 == y.Item2;
        }

        public int GetHashCode((ISymbol, string?) obj)
        {
            unchecked
            {
                return (SymbolEqualityComparer.Default.GetHashCode() * 397) ^ obj.Item2?.GetHashCode() ?? 0;
            }
        }
    }
}