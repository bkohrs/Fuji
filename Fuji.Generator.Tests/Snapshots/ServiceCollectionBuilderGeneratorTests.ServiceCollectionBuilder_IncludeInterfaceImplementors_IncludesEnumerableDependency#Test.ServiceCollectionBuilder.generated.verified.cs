//HintName: Test.ServiceCollectionBuilder.generated.cs
#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace Test
{
    public partial class ServiceCollectionBuilder
    {
        public void Build(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient(typeof(Test.IDependency), typeof(Test.Dependency1));
            serviceCollection.AddTransient(typeof(Test.IDependency), typeof(Test.Dependency2));
        }
    }
}
