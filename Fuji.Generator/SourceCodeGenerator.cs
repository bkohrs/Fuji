using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Fuji;

public class SourceCodeGenerator
{
    private static readonly AssemblyName AssemblyName;
    private readonly ServiceCollectionBuilderDefinition _definition;

    static SourceCodeGenerator()
    {
        AssemblyName = Assembly.GetExecutingAssembly().GetName();
    }

    public SourceCodeGenerator(ServiceCollectionBuilderDefinition definition)
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
               writer.CreateScope($"namespace {_definition.ServiceCollectionBuilderType.ContainingNamespace}"))
        {
            WriteGeneratedCodeAttribute(namespaceScope);
            using (var classScope =
                   namespaceScope.CreateScope($"public partial class {_definition.ServiceCollectionBuilderType.Name}"))
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
                                    service.Key == null
                                        ? $"serviceCollection.AddTransient(typeof({service.InterfaceType.ToDisplayString()}), typeof({service.ImplementationType.ToDisplayString()}));"
                                        : $"serviceCollection.AddKeyedTransient(typeof({service.InterfaceType.ToDisplayString()}), \"{service.Key}\", typeof({service.ImplementationType.ToDisplayString()}));");
                                break;
                            case ServiceLifetime.Singleton:
                                if (service.CustomFactory != null)
                                {
                                    if (service.Key == null)
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
                                        var usesProvider =
                                            service.CustomFactory.Parameters.Length == 2 &&
                                            service.CustomFactory.Parameters[0]
                                                .Type is INamedTypeSymbol providerSymbol &&
                                            service.CustomFactory.Parameters[1].Type is INamedTypeSymbol keySymbol &&
                                            providerSymbol.ToDisplayString() == "System.IServiceProvider" &&
                                            (keySymbol.ToDisplayString() == "object" || keySymbol.ToDisplayString() == "object?");
                                        methodScope.WriteLine(
                                            $"serviceCollection.AddKeyedSingleton<{service.InterfaceType.ToDisplayString()}>(\"{service.Key}\", (provider, key) => {service.CustomFactory.Name}({(usesProvider ? "provider, key" : "")}));");
                                    }
                                }
                                else
                                {
                                    methodScope.WriteLine(
                                        service.Key == null
                                            ? $"serviceCollection.AddSingleton(typeof({service.InterfaceType.ToDisplayString()}), typeof({service.ImplementationType.ToDisplayString()}));"
                                            : $"serviceCollection.AddKeyedSingleton(typeof({service.InterfaceType.ToDisplayString()}), \"{service.Key}\", typeof({service.ImplementationType.ToDisplayString()}));");
                                }
                                break;
                            case ServiceLifetime.Scoped:
                                methodScope.WriteLine(
                                    service.Key == null
                                        ? $"serviceCollection.AddScoped(typeof({service.InterfaceType.ToDisplayString()}), typeof({service.ImplementationType.ToDisplayString()}));"
                                        : $"serviceCollection.AddKeyedScoped(typeof({service.InterfaceType.ToDisplayString()}), \"{service.Key}\", typeof({service.ImplementationType.ToDisplayString()}));");
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

    private static void WriteGeneratedCodeAttribute(ICodeWriterScope scope)
    {
        scope.WriteLine(
            $"[System.CodeDom.Compiler.GeneratedCode(\"{AssemblyName.Name}\",\"{AssemblyName.Version}\")]");
    }
}