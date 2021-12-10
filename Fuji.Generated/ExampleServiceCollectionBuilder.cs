namespace Fuji.Generated;

[ServiceCollectionBuilder]
[ProvideTransient(typeof(ITransientService), typeof(TransientService))]
[ProvideSingleton(typeof(ISingletonService), typeof(SingletonService))]
public partial class ExampleServiceCollectionBuilder {}