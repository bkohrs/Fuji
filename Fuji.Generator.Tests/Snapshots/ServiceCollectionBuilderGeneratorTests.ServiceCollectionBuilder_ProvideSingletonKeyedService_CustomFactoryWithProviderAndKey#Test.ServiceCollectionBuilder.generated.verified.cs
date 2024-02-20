//HintName: Test.ServiceCollectionBuilder.generated.cs
#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace Test
{
    public partial class ServiceCollectionBuilder
    {
        public void Build(IServiceCollection serviceCollection)
        {
            serviceCollection.AddKeyedSingleton<Test.IService>("foo", (provider, key) => CreateService(provider, key));
        }
    }
}
