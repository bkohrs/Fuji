//HintName: Test.ServiceCollectionBuilder.generated.cs
#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace Test
{
    public partial class ServiceCollectionBuilder
    {
        public void Build(IServiceCollection serviceCollection)
        {
            #pragma warning disable CS0612
            serviceCollection.AddTransient(typeof(Test.IService), typeof(Test.Service));
            #pragma warning enable CS0612
        }
    }
}
