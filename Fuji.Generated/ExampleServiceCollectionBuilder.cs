namespace Fuji.Generated;

[ServiceCollectionBuilder]
[ProvideTransient(typeof(ITransientService), typeof(TransientService))]
[ProvideTransient(typeof(TransientAsyncDisposableService))]
[ProvideSingleton(typeof(ISingletonService), typeof(SingletonService))]
[ProvideScoped(typeof(IScopedService), typeof(ScopedService))]
[ProvideScoped(typeof(ScopedAsyncDisposableService))]
public partial class ExampleServiceCollectionBuilder {}