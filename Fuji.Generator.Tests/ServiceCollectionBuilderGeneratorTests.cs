using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace Fuji.Generator.Tests;

[TestFixture]
public class ServiceCollectionBuilderGeneratorTests
{
    public enum GeneratorType
    {
        ServiceCollectionBuilder,
    }

    public enum ServiceLifetime
    {
        None,
        Singleton,
        Scoped,
        Transient,
    }

    public enum DisposeType
    {
        None,
        Sync,
        Async,
    }

    public class ProvidedService
    {
        public ProvidedService(string interfaceType, string implementationType, ServiceLifetime lifetime)
        {
            InterfaceType = interfaceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
        }

        public string InterfaceType { get; }
        public string ImplementationType { get; }
        public ServiceLifetime Lifetime { get; }
    }

    public class GeneratedProvider
    {
        public GeneratedProvider(string className, GeneratorType generatorType, ImmutableArray<ProvidedService> providedServices)
        {
            ClassName = className;
            GeneratorType = generatorType;
            ProvidedServices = providedServices;
        }

        public string ClassName { get; }
        public GeneratorType GeneratorType { get; }
        public ImmutableArray<ProvidedService> ProvidedServices { get; }
    }

    public class Service
    {
        public Service(string interfaceType, string implementationType, ServiceLifetime serviceLifetime,
            ImmutableArray<string> dependencies, DisposeType disposeType = DisposeType.None)
        {
            InterfaceType = interfaceType;
            ImplementationType = implementationType;
            ServiceLifetime = serviceLifetime;
            Dependencies = dependencies;
            DisposeType = disposeType;
        }

        public string InterfaceType { get; }
        public string ImplementationType { get; }
        public ServiceLifetime ServiceLifetime { get; }
        public ImmutableArray<string> Dependencies { get; }
        public DisposeType DisposeType { get; }
    }

    public class GenerateTestCase
    {
        public GenerateTestCase(string name, ImmutableArray<GeneratedProvider> generatedProviders, ImmutableArray<Service> services)
        {
            Name = name;
            GeneratedProviders = generatedProviders;
            Services = services;
        }

        public string Name { get; }
        public ImmutableArray<GeneratedProvider> GeneratedProviders { get; }
        public ImmutableArray<Service> Services { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    private static IEnumerable<T?> GetNullableEnumValues<T>() where T : struct, Enum
    {
        yield return null;
        foreach (var value in Enum.GetValues<T>())
            yield return value;
    }

    public static IEnumerable<GenerateTestCase> GenerateTestCases()
    {
        foreach (var provider in Enum.GetValues<GeneratorType>())
        {
            foreach (var serviceLifetime in Enum.GetValues<ServiceLifetime>().Except(new[] { ServiceLifetime.None }))
            {
                foreach (var serviceHasInterface in new[] { true, false })
                {
                    foreach (var selfDescribedDependency in new[] { true, false })
                    {
                        var dependencyLifetimes = GetNullableEnumValues<ServiceLifetime>()
                            .Except(new ServiceLifetime?[] { ServiceLifetime.None });
                        foreach (var dependencyLifetime in dependencyLifetimes)
                        {
                            foreach (var dependencyHasInterface in new[] { true, false })
                            {
                                if (!dependencyLifetime.HasValue &&
                                    (!selfDescribedDependency || !dependencyHasInterface))
                                {
                                    continue;
                                }
                                var providedServices = ImmutableArray.Create(
                                    new ProvidedService(serviceHasInterface ? "IService1" : "",
                                        "Service1", serviceLifetime));
                                var dependencies = ImmutableArray<string>.Empty;
                                var services = ImmutableArray<Service>.Empty;
                                if (dependencyLifetime.HasValue)
                                {
                                    if (!selfDescribedDependency)
                                    {
                                        providedServices = providedServices.Add(new ProvidedService(
                                            dependencyHasInterface ? "IService2" : "", "Service2",
                                            dependencyLifetime.Value));
                                    }
                                    services = services.Add(new Service(
                                        dependencyHasInterface ? "IService2" : "", "Service2",
                                        selfDescribedDependency ? dependencyLifetime.Value : ServiceLifetime.None,
                                        ImmutableArray<string>.Empty));
                                    dependencies = dependencies.Add(dependencyHasInterface ? "IService2" : "Service2");
                                }
                                services = services.Add(new Service(serviceHasInterface ? "IService1" : "",
                                    "Service1", ServiceLifetime.None, dependencies));
                                var provideName = serviceLifetime.ToString();
                                var serviceType = serviceHasInterface ? "Interface" : "Class";
                                var selfDescribedPrefix = selfDescribedDependency ? "SelfDescribed" : "";
                                var dependency = dependencyLifetime.HasValue
                                    ? (dependencyHasInterface
                                        ? $"With{selfDescribedPrefix}{dependencyLifetime}InterfaceDependency"
                                        : $"With{selfDescribedPrefix}{dependencyLifetime}ClassDependency")
                                    : "NoDependencies";
                                yield return new GenerateTestCase(
                                    $"{provider}_Provide{provideName}_{serviceType}_{dependency}",
                                    ImmutableArray.Create(new GeneratedProvider(provider.ToString(), provider,
                                        providedServices)),
                                    services);
                            }
                        }
                    }
                }
            }
        }

        yield return new GenerateTestCase("ServiceCollectionBuilder_Class_WithMissingDependency",
            ImmutableArray.Create(new GeneratedProvider("ServiceCollectionBuilder", GeneratorType.ServiceCollectionBuilder,
                ImmutableArray.Create(new ProvidedService("", "Service1", ServiceLifetime.Transient)))),
            ImmutableArray.Create(
                new Service("", "Service1", ServiceLifetime.None, ImmutableArray.Create("Service2")))
        );
    }

    [TestCaseSource(nameof(GenerateTestCases))]
    public async Task Generate(GenerateTestCase testCase)
    {
        var code = GenerateCode(testCase);
        await RunGenerator(code).ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_CustomFactory()
    {
        await RunGenerator(@"
namespace Test;

public class Service {}

[Fuji.ServiceCollectionBuilder]
[Fuji.ProvideSingleton(typeof(Service), Factory = nameof(CustomFactory))]
public partial class ServiceCollectionBuilder
{
    private Service CustomFactory()
    {
        return new Service();
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_CustomFactoryWithServiceProvider()
    {
        await RunGenerator(@"
namespace Test;

public class Service {}

[Fuji.ServiceCollectionBuilder]
[Fuji.ProvideSingleton(typeof(Service), Factory = nameof(CustomFactory))]
public partial class ServiceCollectionBuilder
{
    private Service CustomFactory(System.IServiceProvider provider)
    {
        return new Service();
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_ProvidedByCollection()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency {}

public class Service
{
    public Service(IDependency dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.ProvideTransient(typeof(Service))]
[Fuji.ProvidedByCollection(typeof(IDependency))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_IncludeAllServices()
    {
        await RunGenerator(@"
namespace Test;

[Fuji.TransientService]
public class Service {}

[Fuji.ServiceCollectionBuilder(IncludeAllServices = true)]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_ProvideService()
    {
        await RunGenerator(@"
namespace Test;

[Fuji.TransientService]
[Fuji.ProvideService(typeof(ServiceCollectionBuilder))]
public class Service {}

[Fuji.ServiceCollectionBuilder]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }
    [Test]
    public async Task ServiceCollectionBuilder_MultipleServicesWithSameInterface_Priority()
    {
        await RunGenerator(@"
namespace Test;

public interface IService {}
public class Service1 : IService {}
public class Service2 : IService {}
public class Service3 : IService {}

[Fuji.ServiceCollectionBuilder]
[Fuji.ProvideTransient(typeof(IService), typeof(Service1), Priority = 1)]
[Fuji.ProvideTransient(typeof(IService), typeof(Service2), Priority = 2)]
[Fuji.ProvideTransient(typeof(IService), typeof(Service3))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_MultipleSelfDescribedServicesWithSameInterface_Priority()
    {
        await RunGenerator(@"
namespace Test;

public interface IService {}

[Fuji.TransientService(typeof(IService), Priority = 1)]
public class Service1 : IService {}

[Fuji.TransientService(typeof(IService), Priority = 2)]
public class Service2 : IService {}

[Fuji.TransientService(typeof(IService))]
public class Service3 : IService {}

[Fuji.ServiceCollectionBuilder(IncludeAllServices = true)]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_ServiceDependsOnEnumerableOfService()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency {}
public class Dependency1 : IDependency {}
public class Dependency2 : IDependency {}
public interface IService {}
public class Service : IService
{
    public Service(System.Collections.Generic.IEnumerable<IDependency> dependencies) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.ProvideTransient(typeof(IDependency), typeof(Dependency1))]
[Fuji.ProvideTransient(typeof(IDependency), typeof(Dependency2))]
[Fuji.ProvideTransient(typeof(IService), typeof(Service))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_ObsoleteInterface()
    {
        await RunGenerator(@"
namespace Test;

[System.Obsolete]
public interface IService {}
public class Service : IService {}

[Fuji.ServiceCollectionBuilder]
[Fuji.ProvideTransient(typeof(IService), typeof(Service))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_ObsoleteImplementation()
    {
        await RunGenerator(@"
namespace Test;

public interface IService {}
[System.Obsolete]
public class Service : IService {}

[Fuji.ServiceCollectionBuilder]
[Fuji.ProvideTransient(typeof(IService), typeof(Service))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_TransientKeyedService_StringLiteral()
    {
        await RunGenerator(@"
namespace Test;

public interface IService {}
[Fuji.TransientService(typeof(IService), Key = ""foo"")]
public class Service : IService {}

[Fuji.ServiceCollectionBuilder(IncludeAllServices = true)]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_TransientKeyedService_Constant()
    {
        await RunGenerator(@"
namespace Test;

public class Constants
{
    public const string Key = ""foo"";
}

public interface IService {}
[Fuji.TransientService(typeof(IService), Key = Constants.Key)]
public class Service : IService {}

[Fuji.ServiceCollectionBuilder(IncludeAllServices = true)]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_ScopedKeyedService_StringLiteral()
    {
        await RunGenerator(@"
namespace Test;

public interface IService {}
[Fuji.ScopedService(typeof(IService), Key = ""foo"")]
public class Service : IService {}

[Fuji.ServiceCollectionBuilder(IncludeAllServices = true)]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_SingletonKeyedService_StringLiteral()
    {
        await RunGenerator(@"
namespace Test;

public interface IService {}
[Fuji.SingletonService(typeof(IService), Key = ""foo"")]
public class Service : IService {}

[Fuji.ServiceCollectionBuilder(IncludeAllServices = true)]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_ProvideTransientKeyedService_StringLiteral()
    {
        await RunGenerator(@"
namespace Test;

public interface IService {}
public class Service : IService {}

[Fuji.ServiceCollectionBuilder(IncludeAllServices = true)]
[Fuji.ProvideTransient(typeof(IService), typeof(Service), Key = ""foo"")]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_ProvideScopedKeyedService_StringLiteral()
    {
        await RunGenerator(@"
namespace Test;

public interface IService {}
public class Service : IService {}

[Fuji.ServiceCollectionBuilder(IncludeAllServices = true)]
[Fuji.ProvideScoped(typeof(IService), typeof(Service), Key = ""foo"")]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_ProvideSingletonKeyedService_StringLiteral()
    {
        await RunGenerator(@"
namespace Test;

public interface IService {}
public class Service : IService {}

[Fuji.ServiceCollectionBuilder(IncludeAllServices = true)]
[Fuji.ProvideSingleton(typeof(IService), typeof(Service), Key = ""foo"")]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_ProvideSingletonKeyedService_CustomFactory()
    {
        await RunGenerator(@"
namespace Test;

public interface IService {}
public class Service : IService {}

[Fuji.ServiceCollectionBuilder(IncludeAllServices = true)]
[Fuji.ProvideSingleton(typeof(IService), typeof(Service), Factory = nameof(CreateService), Key = ""foo"")]
public partial class ServiceCollectionBuilder
{
    public IService CreateService()
    {
        return new Service();
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_ProvideSingletonKeyedService_CustomFactoryWithProviderAndKey()
    {
        await RunGenerator(@"
#nullable enable
namespace Test;

public interface IService {}
public class Service : IService {}

[Fuji.ServiceCollectionBuilder(IncludeAllServices = true)]
[Fuji.ProvideSingleton(typeof(IService), typeof(Service), Factory = nameof(CreateService), Key = ""foo"")]
public partial class ServiceCollectionBuilder
{
    public IService CreateService(System.IServiceProvider provider, object? key)
    {
        return new Service();
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_SatisfiedKeyedServiceDependency_StringLiteral()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency {}
[Fuji.TransientService(typeof(IDependency), Key = ""foo"")]
public class Dependency : IDependency {}
public interface IService {}
public class Service : IService
{
    public Service([Microsoft.Extensions.DependencyInjection.FromKeyedServices(""foo"")]IDependency dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.ProvideSingleton(typeof(IService), typeof(Service))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_SatisfiedKeyedServiceDependency_Constant()
    {
        await RunGenerator(@"
namespace Test;

public class Constants
{
    public const string Key = ""foo"";
}

public interface IDependency {}
[Fuji.TransientService(typeof(IDependency), Key = Constants.Key)]
public class Dependency : IDependency {}
public interface IService {}
public class Service : IService
{
    public Service([Microsoft.Extensions.DependencyInjection.FromKeyedServices(Constants.Key)]IDependency dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.ProvideSingleton(typeof(IService), typeof(Service))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_SatisfiedKeyedServiceDependencyByProvidedByCollection()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency {}
public interface IService {}
public class Service : IService
{
    public Service([Microsoft.Extensions.DependencyInjection.FromKeyedServices(""foo"")]IDependency dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.ProvideSingleton(typeof(IService), typeof(Service))]
[Fuji.ProvidedByCollection(typeof(IDependency), Key = ""foo"")]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_UnsatisfiedKeyedServiceDependency()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency {}
public interface IService {}
public class Service : IService
{
    public Service([Microsoft.Extensions.DependencyInjection.FromKeyedServices(""foo"")]IDependency dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.ProvideSingleton(typeof(IService), typeof(Service))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_IncludeInterfaceImplementors()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency {}
[Fuji.TransientService(typeof(IDependency))]
public class Dependency {}
public interface IService {}
public class Service : IService
{
    public Service(IDependency dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.IncludeInterfaceImplementors(typeof(IService))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }
    
    [Test]
    public async Task ServiceCollectionBuilder_IncludeInterfaceImplementors_Multiple()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency1 {}
[Fuji.TransientService(typeof(IDependency1))]
public class Dependency1 {}
public interface IService1 {}
public class Service1 : IService1
{
    public Service1(IDependency1 dependency) {}
}
public interface IDependency2 {}
[Fuji.TransientService(typeof(IDependency2))]
public class Dependency2 {}
public interface IService2 {}
public class Service2 : IService2
{
    public Service2(IDependency2 dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.IncludeInterfaceImplementors(typeof(IService1))]
[Fuji.IncludeInterfaceImplementors(typeof(IService2))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_IncludeInterfaceImplementors_IncludesSelfIfAttributed()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency {}
[Fuji.TransientService(typeof(IDependency))]
public class Dependency {}
public interface IService {}
[Fuji.TransientService(typeof(IService))]
public class Service : IService
{
    public Service(IDependency dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.IncludeInterfaceImplementors(typeof(IService))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_IncludeInterfaceImplementors_IncludesEnumerableDependency()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency {}
[Fuji.TransientService(typeof(IDependency))]
public class Dependency1 {}
[Fuji.TransientService(typeof(IDependency))]
public class Dependency2 {}
public interface IService {}
public class Service : IService
{
    public Service(System.Collections.Generic.IEnumerable<IDependency> dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.IncludeInterfaceImplementors(typeof(IService))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_IncludeClassInheritors()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency {}
[Fuji.TransientService(typeof(IDependency))]
public class Dependency {}
public abstract class Grandparent {}
public abstract class Parent : Grandparent {}
public class Service : Parent
{
    public Service(IDependency dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.IncludeClassInheritors(typeof(Grandparent))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_IncludeClassInheritors_Multiple()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency1 {}
[Fuji.TransientService(typeof(IDependency1))]
public class Dependency1 {}
public abstract class Parent1{}
public class Service1 : Parent1
{
    public Service1(IDependency1 dependency) {}
}
public interface IDependency2 {}
[Fuji.TransientService(typeof(IDependency2))]
public class Dependency2 {}
public abstract class Parent2{}
public class Service2 : Parent2
{
    public Service2(IDependency2 dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.IncludeClassInheritors(typeof(Parent1))]
[Fuji.IncludeClassInheritors(typeof(Parent2))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_IncludeClassInheritors_IncludesSelfIfAttributed()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency {}
[Fuji.TransientService(typeof(IDependency))]
public class Dependency {}
public abstract class Grandparent {}
public abstract class Parent : Grandparent {}
[Fuji.TransientService(typeof(Service))]
public class Service : Parent
{
    public Service(IDependency dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.IncludeClassInheritors(typeof(Grandparent))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_IncludeClassInheritors_IncludesEnumerableDependency()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency {}
[Fuji.TransientService(typeof(IDependency))]
public class Dependency1 {}
[Fuji.TransientService(typeof(IDependency))]
public class Dependency2 {}
public abstract class Grandparent {}
public abstract class Parent : Grandparent {}
public class Service : Parent
{
    public Service(System.Collections.Generic.IEnumerable<IDependency> dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.IncludeClassInheritors(typeof(Grandparent))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_IncludeDependency()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency {}
[Fuji.TransientService(typeof(IDependency))]
public class Dependency {}
public interface IService {}
public class Service : IService
{
    public Service(IDependency dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.IncludeDependency(typeof(Service))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_IncludeDependency_IncludesSelfIfAttributed()
    {
        await RunGenerator(@"
namespace Test;

public interface IDependency {}
[Fuji.TransientService(typeof(IDependency))]
public class Dependency {}
public interface IService {}
[Fuji.TransientService(typeof(IDependency))]
public class Service : IService
{
    public Service(IDependency dependency) {}
}

[Fuji.ServiceCollectionBuilder]
[Fuji.IncludeDependency(typeof(Service))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_PropertyInjection()
    {
        await RunGenerator(@"
namespace Test;

public class InjectionAttribute : System.Attribute {}
public interface IDependency {}
[Fuji.TransientService(typeof(IDependency))]
public class Dependency {}
public interface IService {}
public class Service : IService
{
    [Injection]
    public IDependency Dependency { get; }
}

[Fuji.ServiceCollectionBuilder]
[Fuji.IncludeDependency(typeof(Service))]
[Fuji.PropertyInjectionType(typeof(InjectionAttribute))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ServiceCollectionBuilder_PropertyInjection_BaseClass()
    {
        await RunGenerator(@"
namespace Test;

public class InjectionAttribute : System.Attribute {}
public interface IDependency {}
[Fuji.TransientService(typeof(IDependency))]
public class Dependency {}
public interface IService {}
public class Parent
{
    [Injection]
    public IDependency Dependency { get; }
}
public class Service : Parent, IService
{
}

[Fuji.ServiceCollectionBuilder]
[Fuji.IncludeDependency(typeof(Service))]
[Fuji.PropertyInjectionType(typeof(InjectionAttribute))]
public partial class ServiceCollectionBuilder {}
").ConfigureAwait(false);
    }

    private string GenerateCode(GenerateTestCase testCase)
    {
        var builder = new StringBuilder();
        builder.AppendLine("namespace Test;");
        builder.AppendLine();
        foreach (var service in testCase.Services)
        {
            if (!string.IsNullOrWhiteSpace(service.InterfaceType))
            {
                builder.AppendLine($"public interface {service.InterfaceType} {{}}");
                builder.AppendLine();
            }
            if (service.ServiceLifetime != ServiceLifetime.None)
                builder.AppendLine(
                    $"[Fuji.{service.ServiceLifetime}Service{(!string.IsNullOrWhiteSpace(service.InterfaceType) ? $"(typeof({service.InterfaceType}))" : "")}]");
            var interfaces = new List<string>();
            if (!string.IsNullOrWhiteSpace(service.InterfaceType))
                interfaces.Add(service.InterfaceType);
            switch (service.DisposeType)
            {
                case DisposeType.Sync:
                    interfaces.Add("System.IDisposable");
                    break;
                case DisposeType.Async:
                    interfaces.Add("System.IAsyncDisposable");
                    break;
            }
            builder.AppendLine($"public class {service.ImplementationType}{(interfaces.Count > 0 ? $" : {string.Join(", ", interfaces)}" : "")}");
            builder.AppendLine("{");
            if (!service.Dependencies.IsEmpty)
                builder.AppendLine($"    public {service.ImplementationType}({string.Join(", ", service.Dependencies.Select((dependency, index) => $"{dependency} p{index}"))}){{}}");
            switch (service.DisposeType)
            {
                case DisposeType.Sync:
                    builder.AppendLine("    public void Dispose(){}");
                    break;
                case DisposeType.Async:
                    builder.AppendLine("    public ValueTask AsyncDispose(){return default;}");
                    break;
            }
            builder.AppendLine("}");
            builder.AppendLine();
        }
        foreach (var provider in testCase.GeneratedProviders)
        {
            builder.AppendLine($"[Fuji.{provider.GeneratorType}]");
            foreach (var service in provider.ProvidedServices)
            {
                if (service.Lifetime != ServiceLifetime.None)
                    builder.AppendLine($"[Fuji.Provide{service.Lifetime}({(!string.IsNullOrWhiteSpace(service.InterfaceType) ? $"typeof({service.InterfaceType}), " : "")}typeof({service.ImplementationType}))]");
            }
            builder.AppendLine($"public partial class {provider.ClassName}");
            builder.AppendLine("{");
            builder.AppendLine("}");
        }
        return builder.ToString();
    }

    private async Task RunGenerator(string code)
    {
        var referenceAssemblies = await ReferenceAssemblies.Default
            .ResolveAsync(LanguageNames.CSharp, CancellationToken.None).ConfigureAwait(false);
        var assemblies = referenceAssemblies.Add(
            MetadataReference.CreateFromFile(typeof(ServiceCollectionBuilderAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create("name", new[]
        {
            CSharpSyntaxTree.ParseText(@"
namespace Microsoft.Extensions.DependencyInjection;
public class FromKeyedServicesAttribute : System.Attribute
{
    public FromKeyedServicesAttribute(object key){}
}
"),
            CSharpSyntaxTree.ParseText(code),
        }, assemblies);
        var generator = new ServiceCollectionBuilderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);
        await Verify(driver)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("System.CodeDom.Compiler.GeneratedCode")
            .ConfigureAwait(false);
    }
}