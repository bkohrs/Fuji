using Fuji.Generated.Services;

namespace Fuji.Generated;

[ServiceProvider]
[ProvideTransient(typeof(ITransientService), typeof(TransientService))]
[ProvideTransient(typeof(TransientServiceWithTransientDependency))]
[ProvideTransient(typeof(TransientServiceWithSingletonDependency))]
[ProvideTransient(typeof(TransientServiceWithScopedDependency))]
[ProvideTransient(typeof(TransientDisposableService))]
[ProvideTransient(typeof(TransientAsyncDisposableService))]
[ProvideTransient(typeof(ServiceDependsOnSelfDescribed))]
[ProvideTransient(typeof(DependentLibraryService))]
[ProvideTransient(typeof(ServiceDependsOnLibraryService))]
[ProvideSingleton(typeof(ISingletonService), typeof(SingletonService))]
[ProvideSingleton(typeof(SingletonDisposableService))]
[ProvideSingleton(typeof(SingletonAsyncDisposableService))]
[ProvideSingleton(typeof(FactoryProvidedSingleton), Factory = nameof(CreateFactoryProvidedSingleton))]
[ProvideSingleton(typeof(ISelfDescribedPrecedenceService), Factory = nameof(CreatePrecedenceService))]
[ProvideScoped(typeof(IScopedService), typeof(ScopedService))]
[ProvideScoped(typeof(ScopedAsyncDisposableService))]
[ProvideScoped(typeof(ScopedDisposableService))]
[ProvideScoped(typeof(ScopedServiceWithSingletonDependency))]
[ProvideScoped(typeof(ScopedServiceWithTransientDependency))]
[ProvideTransient(typeof(IMultipleImplementationService), typeof(MultipleImplementationService1))]
[ProvideTransient(typeof(IMultipleImplementationService), typeof(MultipleImplementationService2), Priority = 1)]
[ProvideTransient(typeof(ServiceDependsOnEnumerable))]
public partial class ExampleServiceProvider
{
    private FactoryProvidedSingleton CreateFactoryProvidedSingleton()
    {
        return new FactoryProvidedSingleton(true);
    }

    private ISelfDescribedPrecedenceService CreatePrecedenceService()
    {
        return new CustomPrecedenceService();
    }
}

