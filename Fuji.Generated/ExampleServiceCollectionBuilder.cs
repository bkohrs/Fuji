using Fuji.Generated.Services;
using JetBrains.Annotations;

namespace Fuji.Generated;

[ServiceCollectionBuilder]
[ProvideTransient(typeof(ITransientService), typeof(TransientService))]
[ProvideTransient(typeof(TransientAsyncDisposableService))]
[ProvideTransient(typeof(ServiceDependsOnSelfDescribed))]
[ProvideTransient(typeof(DependentLibraryService))]
[ProvideTransient(typeof(ServiceDependsOnLibraryService))]
[ProvideTransient(typeof(ServiceDependentOnCollectionProvided))]
[ProvideSingleton(typeof(ISingletonService), typeof(SingletonService))]
[ProvideSingleton(typeof(FactoryProvidedSingleton), Factory = nameof(CreateFactoryProvidedSingleton))]
[ProvideSingleton(typeof(FactoryProvidedSingletonNeedingServiceProvider), Factory = nameof(CreateFactoryProvidedSingletonNeedingServiceProvider))]
[ProvideScoped(typeof(IScopedService), typeof(ScopedService))]
[ProvideScoped(typeof(ScopedAsyncDisposableService))]
[ProvidedByCollection(typeof(CollectionProvidedService))]
[ProvideTransient(typeof(IMultipleImplementationService), typeof(MultipleImplementationService1))]
[ProvideTransient(typeof(IMultipleImplementationService), typeof(MultipleImplementationService2), Priority = 1)]
[ProvideTransient(typeof(ServiceDependsOnEnumerable))]
public partial class ExampleServiceCollectionBuilder
{
    private FactoryProvidedSingleton CreateFactoryProvidedSingleton()
    {
        return new FactoryProvidedSingleton(true);
    }

    private FactoryProvidedSingletonNeedingServiceProvider CreateFactoryProvidedSingletonNeedingServiceProvider([UsedImplicitly] IServiceProvider provider)
    {
        return new FactoryProvidedSingletonNeedingServiceProvider(true);
    }
}