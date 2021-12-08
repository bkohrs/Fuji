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
[ProvideScoped(typeof(IScopedService), typeof(ScopedService))]
[ProvideScoped(typeof(ScopedAsyncDisposableService))]
[ProvideScoped(typeof(ScopedDisposableService))]
[ProvideScoped(typeof(ScopedServiceWithSingletonDependency))]
[ProvideScoped(typeof(ScopedServiceWithTransientDependency))]
public partial class ExampleServiceProvider {}
