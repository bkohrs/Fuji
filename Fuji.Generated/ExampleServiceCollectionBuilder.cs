using Fuji.Generated.Services;

namespace Fuji.Generated;

[ServiceCollectionBuilder]
[ProvideTransient(typeof(ITransientService), typeof(TransientService))]
[ProvideTransient(typeof(TransientAsyncDisposableService))]
[ProvideTransient(typeof(ServiceDependsOnSelfDescribed))]
[ProvideTransient(typeof(DependentLibraryService))]
[ProvideTransient(typeof(ServiceDependsOnLibraryService))]
[ProvideSingleton(typeof(ISingletonService), typeof(SingletonService))]
[ProvideScoped(typeof(IScopedService), typeof(ScopedService))]
[ProvideScoped(typeof(ScopedAsyncDisposableService))]
public partial class ExampleServiceCollectionBuilder {}