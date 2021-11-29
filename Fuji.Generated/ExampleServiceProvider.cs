namespace Fuji.Generated;

[ServiceProvider]
[ProvideTransient(typeof(ITransientService), typeof(TransientService))]
[ProvideTransient(typeof(TransientServiceWithTransientDependency))]
public partial class ExampleServiceProvider {}
