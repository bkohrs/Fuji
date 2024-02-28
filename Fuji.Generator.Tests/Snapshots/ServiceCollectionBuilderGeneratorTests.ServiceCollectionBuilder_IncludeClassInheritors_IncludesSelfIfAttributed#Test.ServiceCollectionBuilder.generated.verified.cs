﻿//HintName: Test.ServiceCollectionBuilder.generated.cs
#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace Test
{
    public partial class ServiceCollectionBuilder
    {
        public void Build(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient(typeof(Test.Service), typeof(Test.Service));
            serviceCollection.AddTransient(typeof(Test.IDependency), typeof(Test.Dependency));
        }
    }
}
