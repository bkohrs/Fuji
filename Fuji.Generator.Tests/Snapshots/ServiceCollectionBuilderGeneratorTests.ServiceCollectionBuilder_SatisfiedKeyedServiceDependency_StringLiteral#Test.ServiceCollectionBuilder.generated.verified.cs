//HintName: Test.ServiceCollectionBuilder.generated.cs
#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace Test
{
    public partial class ServiceCollectionBuilder
    {
        public void Build(IServiceCollection serviceCollection)
        {
            serviceCollection.AddKeyedTransient(typeof(Test.IDependency), "foo", typeof(Test.Dependency));
            serviceCollection.AddSingleton(typeof(Test.IService), typeof(Test.Service));
        }
    }
}
