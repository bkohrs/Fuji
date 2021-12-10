namespace Fuji.Generated;

[ServiceProvider]
[ProvideTransient(typeof(ITransientService), typeof(TransientService))]
[ProvideTransient(typeof(TransientServiceWithTransientDependency))]
[ProvideTransient(typeof(TransientDisposableService))]
[ProvideTransient(typeof(TransientAsyncDisposableService))]
public partial class ExampleServiceProvider {}
