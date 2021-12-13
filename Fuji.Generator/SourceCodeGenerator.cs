using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Fuji;

public class SourceCodeGenerator
{
    private static readonly AssemblyName AssemblyName;
    private readonly ServiceProviderDefinition _definition;

    static SourceCodeGenerator()
    {
        AssemblyName = Assembly.GetExecutingAssembly().GetName();
    }

    public SourceCodeGenerator(ServiceProviderDefinition definition)
    {
        _definition = definition;
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
                    foreach (var service in _definition.ProvidedServices)
                    {
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
                    }
                }
            }
        }
        return writer.ToString();
    }

    public string GenerateServiceProvider()
    {
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
                WriteServiceFields(classScope, _definition.ProvidedServices.Where(service =>
                    service.Lifetime != ServiceLifetime.Transient));
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
                    foreach (var service in _definition.ProvidedServices)
                    {
                        WriteServiceFactoryInitialization(methodScope, service);
                    }
                }
                using (var methodScope = classScope.CreateScope("public object? GetService(Type serviceType)"))
                {
                    methodScope.WriteLine("return _factory.TryGetValue(serviceType, out var func) ? func() : null;");
                }
                using (var methodScope = classScope.CreateScope("public bool IsService(Type serviceType)"))
                {
                    methodScope.WriteLine("return _factory.ContainsKey(serviceType);");
                }

                foreach (var service in _definition.ProvidedServices)
                {
                    WriteServiceFactoryMethod(classScope, service);
                    WriteServiceResolutionMethod(classScope, service, ResolutionTarget.Root);
                }
                using (var nestedClassScope = classScope.CreateScope(
                           "protected class Scope : System.IServiceProvider, System.IAsyncDisposable, Microsoft.Extensions.DependencyInjection.IServiceScope"))
                {
                    nestedClassScope.WriteLine($"private readonly {_definition.ServiceProviderType.ToDisplayString()} _root;");
                    WriteDisposableCollections(nestedClassScope);
                    WriteFactoryDeclaration(nestedClassScope);
                    WriteServiceFields(nestedClassScope, _definition.ProvidedServices.Where(service =>
                        service.Lifetime == ServiceLifetime.Scoped));
                    using (var methodScope = nestedClassScope.CreateScope($"public Scope({_definition.ServiceProviderType.ToDisplayString()} root)"))
                    {
                        methodScope.WriteLine("_root = root;");
                        foreach (var service in _definition.ProvidedServices.Where(service =>
                                     service.Lifetime != ServiceLifetime.Singleton))
                        {
                            WriteServiceFactoryInitialization(methodScope, service);
                        }
                    }
                    nestedClassScope.WriteLine("public System.IServiceProvider ServiceProvider => this;");
                    WriteAddDisposableMethods(nestedClassScope);
                    using (var methodScope = nestedClassScope.CreateScope("public object? GetService(Type serviceType)"))
                    {
                        methodScope.WriteLine("return _factory.TryGetValue(serviceType, out var func) ? func() : _root.GetService(serviceType);");
                    }
                    foreach (var service in _definition.ProvidedServices)
                    {
                        WriteServiceFactoryMethod(nestedClassScope, service);
                        WriteServiceResolutionMethod(nestedClassScope, service, ResolutionTarget.Scope);
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

    private string GetFactoryMethodName(InjectableService service)
    {
        if (service.CustomFactory != null)
            return service.CustomFactory.Name;
        if (service.Lifetime != ServiceLifetime.Transient)
            return $"Create{service.InterfaceType.ToDisplayString().Replace(".", "_")}";
        return GetResolutionMethodName(service.InterfaceType);
    }

    private string GetResolutionMethodName(INamedTypeSymbol namedTypeSymbol)
    {
        return $"Get{namedTypeSymbol.ToDisplayString().Replace(".", "_")}";
    }

    private static string GetServiceFieldName(INamedTypeSymbol namedTypeSymbol)
    {
        return "_" + namedTypeSymbol.ToDisplayString().Replace(".", "_");
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

    private void WriteServiceFactoryInitialization(ICodeWriterScope scope, InjectableService service)
    {
        if (service.Lifetime != ServiceLifetime.Transient)
            scope.WriteLine($"{GetServiceFieldName(service.InterfaceType)} = new System.Lazy<{service.InterfaceType}>({GetFactoryMethodName(service)});");
        scope.WriteLine(
            $"_factory[typeof({service.InterfaceType.ToDisplayString()})] = {GetResolutionMethodName(service.InterfaceType)};");
    }

    private void WriteServiceFactoryMethod(ICodeWriterScope scope, InjectableService service)
    {
        if (service.CustomFactory != null)
            return;

        using var methodScope = scope.CreateScope(
            $"private {service.InterfaceType.ToDisplayString()} {GetFactoryMethodName(service)}()");
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
                $"    {GetResolutionMethodName(parameter)}(){(i == service.ConstructorArguments.Length - 1 ? "" : ",")}");
            foreach (var parameter in parameters)
                methodScope.WriteLine(parameter);
            methodScope.WriteLine($"){disposableSuffix};");
        }
        else
            methodScope.WriteLine(
                $"return {disposablePrefix}new {service.ImplementationType.ToDisplayString()}(){disposableSuffix};");
    }

    private static void WriteServiceFields(ICodeWriterScope scope, IEnumerable<InjectableService> services)
    {
        foreach (var service in services)
        {
            scope.WriteLine(
                $"private readonly System.Lazy<{service.InterfaceType}> {GetServiceFieldName(service.InterfaceType)};");
        }
    }

    private void WriteServiceResolutionMethod(ICodeWriterScope scope, InjectableService service,
        ResolutionTarget resolutionTarget)
    {
        if (service.Lifetime == ServiceLifetime.Transient)
            return;

        using var methodScope = scope.CreateScope(
            $"private {service.InterfaceType.ToDisplayString()} {GetResolutionMethodName(service.InterfaceType)}()");
        if (service.Lifetime == ServiceLifetime.Singleton && resolutionTarget == ResolutionTarget.Scope)
            methodScope.WriteLine($"return _root.{GetResolutionMethodName(service.InterfaceType)}();");
        else
            methodScope.WriteLine($"return {GetServiceFieldName(service.InterfaceType)}.Value;");
    }

    private enum ResolutionTarget
    {
        Root,
        Scope,
    }
}