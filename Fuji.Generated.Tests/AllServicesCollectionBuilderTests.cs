using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Fuji.Generated.Tests;

[TestFixture]
public class AllServicesCollectionBuilderTests
{
    [Test]
    public void IncludesAllServices()
    {
        var collection = new ServiceCollection();
        var builder = new AllServicesCollectionBuilder();
        builder.Build(collection);
        var provider = collection.BuildServiceProvider();
        var service = provider.GetService<SelfDescribedTransientService>();
        Assert.That(service, Is.Not.Null);
    }
}