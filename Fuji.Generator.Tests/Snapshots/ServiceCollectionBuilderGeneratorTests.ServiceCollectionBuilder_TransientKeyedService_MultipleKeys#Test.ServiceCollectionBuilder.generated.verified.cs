﻿//HintName: Test.ServiceCollectionBuilder.generated.cs
#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace Test
{
    public partial class ServiceCollectionBuilder
    {
        public void Build(IServiceCollection serviceCollection)
        {
            serviceCollection.AddKeyedTransient(typeof(Test.IService), "foo", typeof(Test.Service));
            serviceCollection.AddKeyedTransient(typeof(Test.IService), "bar", typeof(Test.Service));
        }
    }
}
