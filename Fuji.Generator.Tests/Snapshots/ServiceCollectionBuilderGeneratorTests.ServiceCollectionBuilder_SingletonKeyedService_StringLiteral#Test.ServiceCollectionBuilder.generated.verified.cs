﻿//HintName: Test.ServiceCollectionBuilder.generated.cs
#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace Test
{
    public partial class ServiceCollectionBuilder
    {
        public void Build(IServiceCollection serviceCollection)
        {
            serviceCollection.AddKeyedSingleton(typeof(Test.IService), "foo", typeof(Test.Service));
        }
    }
}
