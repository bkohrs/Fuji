namespace Fuji.Generated;

[ServiceProvider]
[ProvideTransient(typeof(ITransientService), typeof(TransientService))]
[ProvideTransient(typeof(TransientServiceWithTransientDependency))]
[ProvideTransient(typeof(TransientServiceWithSingletonDependency))]
[ProvideTransient(typeof(TransientDisposableService))]
[ProvideTransient(typeof(TransientAsyncDisposableService))]
[ProvideSingleton(typeof(ISingletonService), typeof(SingletonService))]
[ProvideSingleton(typeof(SingletonDisposableService))]
[ProvideSingleton(typeof(SingletonAsyncDisposableService))]
public partial class ExampleServiceProvider {}
