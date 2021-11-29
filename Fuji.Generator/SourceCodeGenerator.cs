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

    public string GenerateServiceProvider()
    {
        var writer = new CodeWriter();
        writer.WriteLine("#nullable enable");
        writer.WriteLine("");
        using (var namespaceScope = writer.CreateScope($"namespace {_definition.ServiceProviderType.ContainingNamespace}"))
        {
            namespaceScope.WriteLine(
                $"[System.CodeDom.Compiler.GeneratedCode(\"{AssemblyName.Name}\",\"{AssemblyName.Version}\")]");
            using (var classScope =
                   namespaceScope.CreateScope(
                       $"public partial class {_definition.ServiceProviderType.Name} : System.IServiceProvider"))
            {
                classScope.WriteLine(
                    "private readonly System.Collections.Generic.Dictionary<Type, Func<object>> _factory = new();");
                classScope.WriteLine("");
                using (var methodScope = classScope.CreateScope($"public {_definition.ServiceProviderType.Name}()"))
                {
                    foreach (var service in _definition.ProvidedServices)
                        methodScope.WriteLine(
                            $"_factory[typeof({service.InterfaceType.ToDisplayString()})] = {GetFactoryMethodName(service.InterfaceType)};");
                }
                using (var methodScope = classScope.CreateScope("public object? GetService(Type serviceType)"))
                {
                    methodScope.WriteLine("return _factory.TryGetValue(serviceType, out var func) ? func() : null;");
                }

                foreach (var service in _definition.ProvidedServices)
                {
                    using var methodScope = classScope.CreateScope(
                        $"private {service.InterfaceType.ToDisplayString()} {GetFactoryMethodName(service.InterfaceType)}()");
                    if (service.ConstructorArguments.Length > 0)
                    {
                        methodScope.WriteLine($"return new {service.ImplementationType.ToDisplayString()}(");
                        var parameters = service.ConstructorArguments.Select((parameter, i) =>
                            $"    {GetFactoryMethodName(parameter)}(){(i == service.ConstructorArguments.Length - 1 ? "" : ",")}");
                        foreach (var parameter in parameters)
                            methodScope.WriteLine(parameter);
                        methodScope.WriteLine(");");
                    }
                    else
                        methodScope.WriteLine($"return new {service.ImplementationType.ToDisplayString()}();");
                }
            }
        }
        return writer.ToString();
    }

    private string GetFactoryMethodName(INamedTypeSymbol namedTypeSymbol)
    {
        return $"Create{namedTypeSymbol.ToDisplayString().Replace(".", "_")}";
    }
}