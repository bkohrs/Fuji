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
                                methodScope.WriteLine(
                                    $"serviceCollection.AddSingleton(typeof({service.InterfaceType.ToDisplayString()}), typeof({service.ImplementationType.ToDisplayString()}));");
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
                       $"public partial class {_definition.ServiceProviderType.Name} : System.IServiceProvider"))
            {
                classScope.WriteLine("private readonly System.Collections.Concurrent.ConcurrentBag<IAsyncDisposable> _asyncDisposables = new();");
                classScope.WriteLine("private readonly System.Collections.Concurrent.ConcurrentBag<IDisposable> _disposables = new();");
                classScope.WriteLine(
                    "private readonly System.Collections.Generic.Dictionary<Type, Func<object>> _factory = new();");
                foreach (var singleton in _definition.ProvidedServices.Where(service =>
                             service.Lifetime == ServiceLifetime.Singleton))
                {
                    classScope.WriteLine($"private readonly System.Lazy<{singleton.InterfaceType}> {GetServiceFieldName(singleton.InterfaceType)};");
                }
                classScope.WriteLine("");
                using (var methodScope =
                       classScope.CreateScope("protected T AddAsyncDisposable<T>(T asyncDisposable) where T: System.IAsyncDisposable"))
                {
                    methodScope.WriteLine("_asyncDisposables.Add(asyncDisposable);");
                    methodScope.WriteLine("return asyncDisposable;");
                }
                using (var methodScope =
                       classScope.CreateScope("protected T AddDisposable<T>(T disposable) where T: System.IDisposable"))
                {
                    methodScope.WriteLine("_disposables.Add(disposable);");
                    methodScope.WriteLine("return disposable;");
                }
                using (var methodScope = classScope.CreateScope("public async System.Threading.Tasks.ValueTask DisposeAsync()"))
                {
                    methodScope.WriteLine("foreach (var disposable in _disposables)");
                    methodScope.WriteLine("    disposable.Dispose();");
                    methodScope.WriteLine("_disposables.Clear();");
                    methodScope.WriteLine("foreach (var disposable in _asyncDisposables)");
                    methodScope.WriteLine("    await disposable.DisposeAsync().ConfigureAwait(false);");
                    methodScope.WriteLine("_asyncDisposables.Clear();");
                }
                using (var methodScope = classScope.CreateScope($"public {_definition.ServiceProviderType.Name}()"))
                {
                    foreach (var service in _definition.ProvidedServices)
                    {
                        if (service.Lifetime == ServiceLifetime.Singleton)
                            methodScope.WriteLine($"{GetServiceFieldName(service.InterfaceType)} = new System.Lazy<{service.InterfaceType}>({GetFactoryMethodName(service)});");
                        methodScope.WriteLine(
                            $"_factory[typeof({service.InterfaceType.ToDisplayString()})] = {GetResolutionMethodName(service.InterfaceType)};");

                    }
                }
                using (var methodScope = classScope.CreateScope("public object? GetService(Type serviceType)"))
                {
                    methodScope.WriteLine("return _factory.TryGetValue(serviceType, out var func) ? func() : null;");
                }

                foreach (var service in _definition.ProvidedServices)
                {
                    using (var methodScope = classScope.CreateScope(
                               $"private {service.InterfaceType.ToDisplayString()} {GetFactoryMethodName(service)}()"))
                    {
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
                            methodScope.WriteLine(
                                $"return {disposablePrefix}new {service.ImplementationType.ToDisplayString()}(");
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
                    if (service.Lifetime == ServiceLifetime.Singleton)
                    {
                        using var methodScope = classScope.CreateScope(
                            $"private {service.InterfaceType.ToDisplayString()} {GetResolutionMethodName(service.InterfaceType)}()");
                        methodScope.WriteLine($"return {GetServiceFieldName(service.InterfaceType)}.Value;");
                    }
                }
            }
        }
        return writer.ToString();
    }

    private string GetFactoryMethodName(InjectableService service)
    {
        if (service.Lifetime == ServiceLifetime.Singleton)
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

    private static void WriteGeneratedCodeAttribute(ICodeWriterScope scope)
    {
        scope.WriteLine(
            $"[System.CodeDom.Compiler.GeneratedCode(\"{AssemblyName.Name}\",\"{AssemblyName.Version}\")]");
    }
}