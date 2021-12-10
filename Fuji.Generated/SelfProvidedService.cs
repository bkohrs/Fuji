namespace Fuji.Generated;

[ProvideService(typeof(ExampleServiceProvider))]
[ProvideService(typeof(ExampleServiceCollectionBuilder))]
[TransientService]
public class SelfProvidedService {}