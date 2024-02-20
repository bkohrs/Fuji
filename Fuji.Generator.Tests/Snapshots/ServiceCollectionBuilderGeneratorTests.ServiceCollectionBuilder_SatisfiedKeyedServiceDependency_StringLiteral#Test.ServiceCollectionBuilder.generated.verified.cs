﻿//HintName: Test.ServiceCollectionBuilder.generated.cs
#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace Test
{
    public partial class ServiceCollectionBuilder
    {
        public void Build(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(typeof(Test.IService), typeof(Test.Service));
            serviceCollection.AddKeyedTransient(typeof(Test.IDependency), "foo", typeof(Test.Dependency));
        }
    }
}
