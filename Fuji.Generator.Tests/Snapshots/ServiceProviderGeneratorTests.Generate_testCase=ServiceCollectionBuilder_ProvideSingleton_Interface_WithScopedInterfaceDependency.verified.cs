﻿//HintName: Test.ServiceCollectionBuilder.generated.cs
#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace Test
{
    public partial class ServiceCollectionBuilder
    {
        public void Build(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(typeof(Test.IService1), typeof(Test.Service1));
            serviceCollection.AddScoped(typeof(Test.IService2), typeof(Test.Service2));
        }
    }
}
