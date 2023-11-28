using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Fuji;

public class SourceCodeGenerator
{
    private static readonly AssemblyName AssemblyName;
    private readonly ServiceProviderDefinition _definition;
    private readonly INamedTypeSymbol _enumerableSymbol;

    static SourceCodeGenerator()
    {
        AssemblyName = Assembly.GetExecutingAssembly().GetName();
    }

    public SourceCodeGenerator(ServiceProviderDefinition definition, INamedTypeSymbol enumerableSymbol)
    {
        _definition = definition;
        _enumerableSymbol = enumerableSymbol;
    }

    public string GenerateServiceCollectionBuilder()
    {
        var writer = new CodeWriter();
        writer.WriteLine("#nullable enable");
        writer.WriteLine("using Microsoft.Extensions.DependencyInjection;");
        writer.WriteLine("");
        using (var namespaceScope =
               writer.CreateScope($"namespace {_definition.ServiceProviderType.ContainingNamespace}"))
        {
            WriteGeneratedCodeAttribute(namespaceScope);
            using (var classScope =
                   namespaceScope.CreateScope($"public partial class {_definition.ServiceProviderType.Name}"))
            {
                using (var methodScope =
                       classScope.CreateScope("public void Build(IServiceCollection serviceCollection)"))
                {
                    foreach (var service in _definition.ProvidedServices.OrderBy(service => service.Priority))
                    {
                        if (service.HasObsoleteAttribute)
                            methodScope.WriteLine("#pragma warning disable CS0612");
                        switch (service.Lifetime)
                        {
                            case ServiceLifetime.Transient:
                                methodScope.WriteLine(
                                    $"serviceCollection.AddTransient(typeof({service.InterfaceType.ToDisplayString()}), typeof({service.ImplementationType.ToDisplayString()}));");
                                break;
                            case ServiceLifetime.Singleton:
                                if (service.CustomFactory != null)
                                {
                                    var usesProvider =
                                        service.CustomFactory.Parameters.Length == 1 &&
                                        service.CustomFactory.Parameters[0].Type is INamedTypeSymbol paramSymbol &&
                                        paramSymbol.ToDisplayString() == "System.IServiceProvider";
                                    methodScope.WriteLine(
                                        $"serviceCollection.AddSingleton<{service.InterfaceType.ToDisplayString()}>(provider => {service.CustomFactory.Name}({(usesProvider ? "provider" : "")}));");
                                }
                                else
                                {
                                    methodScope.WriteLine(
                                        $"serviceCollection.AddSingleton(typeof({service.InterfaceType.ToDisplayString()}), typeof({service.ImplementationType.ToDisplayString()}));");
                                }
                                break;
                            case ServiceLifetime.Scoped:
                                methodScope.WriteLine(
                                    $"serviceCollection.AddScoped(typeof({service.InterfaceType.ToDisplayString()}), typeof({service.ImplementationType.ToDisplayString()}));");
                                break;
                        }
                        if (service.HasObsoleteAttribute)
                            methodScope.WriteLine("#pragma warning restore CS0612");
                    }
                }
            }
        }
        return writer.ToString();
    }

    public string GenerateServiceProvider()
    {
        var services = _definition.ProvidedServices
            .OrderByDescending(service => service.Priority)
            .GroupBy(service => service.InterfaceType, SymbolEqualityComparer.Default)
            .SelectMany(group => group.Select((service, index) => (Service: service, IsPrimary: index == 0)))
            .ToImmutableArray();
        var serviceLookup =
            services.ToLookup<(InjectableService Service, bool IsPrimary), ITypeSymbol>(
                service => service.Service.InterfaceType, SymbolEqualityComparer.Default);

        var writer = new CodeWriter();
        writer.WriteLine("#nullable enable");
        writer.WriteLine("");

        using (var namespaceScope = writer.CreateScope($"namespace {_definition.ServiceProviderType.ContainingNamespace}"))
        {
            WriteGeneratedCodeAttribute(namespaceScope);
            using (var classScope =
                   namespaceScope.CreateScope(
                       $"public partial class {_definition.ServiceProviderType.Name} : System.IServiceProvider, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory,  Microsoft.Extensions.DependencyInjection.IServiceProviderIsService"))
            {
                WriteDisposableCollections(classScope);
                WriteFactoryDeclaration(classScope);
                WriteServiceFields(classScope, services.Where(service =>
                    service.Service.Lifetime != ServiceLifetime.Transient));
                classScope.WriteLine("");
                WriteAddDisposableMethods(classScope);
                using (var methodScope =
                       classScope.CreateScope("public Microsoft.Extensions.DependencyInjection.IServiceScope CreateScope()"))
                {
                    methodScope.WriteLine("return new Scope(this);");
                }

                WriteDisposeAsyncMethod(classScope);
                using (var methodScope = classScope.CreateScope($"public {_definition.ServiceProviderType.Name}()"))
                {
                    methodScope.WriteLine(
                        "_factory[typeof(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory)] = () => this;");
                    foreach (var service in services)
                        WriteServiceFactoryInitialization(methodScope, service.Service, service.IsPrimary);
                    foreach (var group in serviceLookup)
                        WriteEnumerableFactoryInitialization(methodScope, group);
                }
                using (var methodScope = classScope.CreateScope("public object? GetService(Type serviceType)"))
                {
                    methodScope.WriteLine("return _factory.TryGetValue(serviceType, out var func) ? func() : null;");
                }
                using (var methodScope = classScope.CreateScope("public bool IsService(Type serviceType)"))
                {
                    methodScope.WriteLine("return _factory.ContainsKey(serviceType);");
                }

                foreach (var service in services)
                {
                    WriteServiceFactoryMethod(classScope, service.Service, service.IsPrimary, serviceLookup);
                    WriteServiceResolutionMethod(classScope, service.Service, service.IsPrimary, ResolutionTarget.Root);
                }
                foreach (var group in serviceLookup)
                {
                    WriteEnumerableResolutionMethod(classScope, group, serviceLookup);
                }
                using (var nestedClassScope = classScope.CreateScope(
                           "protected class Scope : System.IServiceProvider, System.IAsyncDisposable, Microsoft.Extensions.DependencyInjection.IServiceScope"))
                {
                    nestedClassScope.WriteLine($"private readonly {_definition.ServiceProviderType.ToDisplayString()} _root;");
                    WriteDisposableCollections(nestedClassScope);
                    WriteFactoryDeclaration(nestedClassScope);
                    WriteServiceFields(nestedClassScope, services.Where(service =>
                        service.Service.Lifetime == ServiceLifetime.Scoped));
                    using (var methodScope = nestedClassScope.CreateScope($"public Scope({_definition.ServiceProviderType.ToDisplayString()} root)"))
                    {
                        methodScope.WriteLine("_root = root;");
                        foreach (var service in services.Where(service =>
                                     service.Service.Lifetime != ServiceLifetime.Singleton))
                        {
                            WriteServiceFactoryInitialization(methodScope, service.Service, service.IsPrimary);
                        }
                        foreach (var group in serviceLookup)
                            WriteEnumerableFactoryInitialization(methodScope, group);
                    }
                    nestedClassScope.WriteLine("public System.IServiceProvider ServiceProvider => this;");
                    WriteAddDisposableMethods(nestedClassScope);
                    using (var methodScope = nestedClassScope.CreateScope("public object? GetService(Type serviceType)"))
                    {
                        methodScope.WriteLine("return _factory.TryGetValue(serviceType, out var func) ? func() : _root.GetService(serviceType);");
                    }
                    foreach (var service in services)
                    {
                        WriteServiceFactoryMethod(nestedClassScope, service.Service, service.IsPrimary, serviceLookup);
                        WriteServiceResolutionMethod(nestedClassScope, service.Service, service.IsPrimary, ResolutionTarget.Scope);
                    }
                    foreach (var group in serviceLookup)
                    {
                        WriteEnumerableResolutionMethod(nestedClassScope, group, serviceLookup);
                    }
                    WriteDisposeAsyncMethod(nestedClassScope);
                    using (var methodScope =
                           nestedClassScope.CreateScope("public void Dispose()"))
                    {
                        methodScope.WriteLine("throw new System.NotSupportedException(\"Dispose() is not supported on scopes. Use DisposeAsync() instead.\");");
                    }
                }
            }
        }
        return writer.ToString();
    }

    private string GetEnumerableResolutionMethodName(ITypeSymbol serviceType)
    {
        return $"GetEnumerable{serviceType.ToDisplayString().Replace(".", "_")}";
    }

    private string GetFactoryMethodName(InjectableService service, bool isPrimary)
    {
        if (service.CustomFactory != null)
            return service.CustomFactory.Name;
        if (service.Lifetime != ServiceLifetime.Transient)
            return $"Create{(isPrimary ? service.InterfaceType : service.ImplementationType).ToDisplayString().Replace(".", "_")}";
        return GetResolutionMethodName(service, isPrimary);
    }

    private string GetResolutionMethodName(InjectableService service, bool isPrimary)
    {
        return $"Get{(isPrimary ? service.InterfaceType : service.ImplementationType).ToDisplayString().Replace(".", "_")}";
    }

    private static string GetServiceFieldName(InjectableService service, bool isPrimary)
    {
        return "_" + (isPrimary ? service.InterfaceType : service.ImplementationType).ToDisplayString()
            .Replace(".", "_");
    }

    private static void WriteAddDisposableMethods(ICodeWriterScope scope)
    {
        using (var methodScope =
               scope.CreateScope(
                   "protected T AddAsyncDisposable<T>(T asyncDisposable) where T: System.IAsyncDisposable"))
        {
            methodScope.WriteLine("_asyncDisposables.Add(asyncDisposable);");
            methodScope.WriteLine("return asyncDisposable;");
        }
        using (var methodScope =
               scope.CreateScope("protected T AddDisposable<T>(T disposable) where T: System.IDisposable"))
        {
            methodScope.WriteLine("_disposables.Add(disposable);");
            methodScope.WriteLine("return disposable;");
        }
    }

    private static void WriteDisposableCollections(ICodeWriterScope scope)
    {
        scope.WriteLine(
            "private readonly System.Collections.Concurrent.ConcurrentBag<IAsyncDisposable> _asyncDisposables = new();");
        scope.WriteLine(
            "private readonly System.Collections.Concurrent.ConcurrentBag<IDisposable> _disposables = new();");
    }

    private static void WriteDisposeAsyncMethod(ICodeWriterScope scope)
    {
        using (var methodScope = scope.CreateScope("public async System.Threading.Tasks.ValueTask DisposeAsync()"))
        {
            methodScope.WriteLine("foreach (var disposable in _disposables)");
            methodScope.WriteLine("    disposable.Dispose();");
            methodScope.WriteLine("_disposables.Clear();");
            methodScope.WriteLine("foreach (var disposable in _asyncDisposables)");
            methodScope.WriteLine("    await disposable.DisposeAsync().ConfigureAwait(false);");
            methodScope.WriteLine("_asyncDisposables.Clear();");
        }
    }

    private void WriteEnumerableFactoryInitialization(ICodeWriterScope scope, IGrouping<ITypeSymbol,(InjectableService Service, bool IsPrimary)> group)
    {
        scope.WriteLine($"_factory[typeof(System.Collections.Generic.IEnumerable<{group.Key.ToDisplayString()}>)] = {GetEnumerableResolutionMethodName(group.Key)};");
    }

    private void WriteEnumerableResolutionMethod(ICodeWriterScope scope,
        IGrouping<ITypeSymbol, (InjectableService Service, bool IsPrimary)> @group,
        ILookup<ITypeSymbol, (InjectableService Service, bool IsPrimary)> serviceLookup)
    {
        using var methodScope = scope.CreateScope($"private System.Collections.Generic.IEnumerable<{group.Key.ToDisplayString()}> {GetEnumerableResolutionMethodName(group.Key)}()");

        var elementType = group.Key;
        var parameterServices = serviceLookup[elementType].ToImmutableArray();
        var argumentString = string.Join(", ",
            parameterServices.Select(paramService =>
                $"{GetResolutionMethodName(paramService.Service, paramService.IsPrimary)}()"));
        methodScope.WriteLine($"return new {elementType.ToDisplayString()}[{(!parameterServices.Any() ? "0" : "")}]{{{argumentString}}};");
    }

    private static void WriteFactoryDeclaration(ICodeWriterScope scope)
    {
        scope.WriteLine(
            "private readonly System.Collections.Generic.Dictionary<Type, Func<object>> _factory = new();");
    }

    private static void WriteGeneratedCodeAttribute(ICodeWriterScope scope)
    {
        scope.WriteLine(
            $"[System.CodeDom.Compiler.GeneratedCode(\"{AssemblyName.Name}\",\"{AssemblyName.Version}\")]");
    }

    private void WriteServiceFactoryInitialization(ICodeWriterScope scope, InjectableService service, bool isPrimary)
    {
        if (service.Lifetime != ServiceLifetime.Transient)
            scope.WriteLine($"{GetServiceFieldName(service, isPrimary)} = new System.Lazy<{service.InterfaceType}>({GetFactoryMethodName(service, isPrimary)});");
        if (isPrimary)
            scope.WriteLine($"_factory[typeof({service.InterfaceType.ToDisplayString()})] = {GetResolutionMethodName(service, isPrimary)};");
    }

    private void WriteServiceFactoryMethod(ICodeWriterScope scope, InjectableService service, bool isPrimary,
        ILookup<ITypeSymbol, (InjectableService Service, bool IsPrimary)> serviceLookup)
    {
        if (service.CustomFactory != null)
            return;

        using var methodScope = scope.CreateScope(
            $"private {service.InterfaceType.ToDisplayString()} {GetFactoryMethodName(service, isPrimary)}()");
        var disposablePrefix = service.DisposeType switch
        {
            DisposeType.Async => "AddAsyncDisposable(",
            DisposeType.Sync => "AddDisposable(",
            _ => "",
        };
        var disposableSuffix = service.DisposeType switch
        {
            DisposeType.None => "",
            _ => ")",
        };
        if (service.ConstructorArguments.Length > 0)
        {
            methodScope.WriteLine($"return {disposablePrefix}new {service.ImplementationType.ToDisplayString()}(");
            var parameters = service.ConstructorArguments.Select((parameter, i) =>
            {
                if (parameter.IsGenericType &&
                    SymbolEqualityComparer.Default.Equals(_enumerableSymbol, parameter.OriginalDefinition))
                {
                    var elementType = parameter.TypeArguments[0];
                    return
                        $"    {GetEnumerableResolutionMethodName(elementType)}(){(i == service.ConstructorArguments.Length - 1 ? "" : ",")}";
                }
                var paramService = serviceLookup[parameter].Single(svc => svc.IsPrimary);
                return
                    $"    {GetResolutionMethodName(paramService.Service, paramService.IsPrimary)}(){(i == service.ConstructorArguments.Length - 1 ? "" : ",")}";
            });
            foreach (var parameter in parameters)
                methodScope.WriteLine(parameter);
            methodScope.WriteLine($"){disposableSuffix};");
        }
        else
            methodScope.WriteLine(
                $"return {disposablePrefix}new {service.ImplementationType.ToDisplayString()}(){disposableSuffix};");
    }

    private static void WriteServiceFields(ICodeWriterScope scope, IEnumerable<(InjectableService Service, bool IsPrimary)> services)
    {
        foreach (var service in services)
        {
            scope.WriteLine(
                $"private readonly System.Lazy<{service.Service.InterfaceType}> {GetServiceFieldName(service.Service, service.IsPrimary)};");
        }
    }

    private void WriteServiceResolutionMethod(ICodeWriterScope scope, InjectableService service, bool isPrimary,
        ResolutionTarget resolutionTarget)
    {
        if (service.Lifetime == ServiceLifetime.Transient)
            return;

        using var methodScope = scope.CreateScope(
            $"private {service.InterfaceType.ToDisplayString()} {GetResolutionMethodName(service, isPrimary)}()");
        if (service.Lifetime == ServiceLifetime.Singleton && resolutionTarget == ResolutionTarget.Scope)
            methodScope.WriteLine($"return _root.{GetResolutionMethodName(service, isPrimary)}();");
        else
            methodScope.WriteLine($"return {GetServiceFieldName(service, isPrimary)}.Value;");
    }

    private enum ResolutionTarget
    {
        Root,
        Scope,
    }
}