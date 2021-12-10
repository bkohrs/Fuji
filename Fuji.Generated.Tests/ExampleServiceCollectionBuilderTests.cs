using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Fuji.Generated.Tests;

[TestFixture]
public class ExampleServiceCollectionBuilderTests
{
    private IServiceProvider? _provider;

    [SetUp]
    public void SetUp()
    {
        var collection = new ServiceCollection();
        new ExampleServiceCollectionBuilder().Build(collection);
        _provider = collection.BuildServiceProvider();
    }

    [Test]
    public void TransientService()
    {
        var service1 = _provider?.GetService<ITransientService>();
        Assert.That(service1, Is.Not.Null);
        Assert.That(service1, Is.TypeOf<TransientService>());

        var service2 = _provider?.GetService<ITransientService>();
        Assert.That(service2, Is.Not.Null);
        Assert.That(service2, Is.TypeOf<TransientService>());
        Assert.That(service2, Is.Not.SameAs(service1));
    }

    [Test]
    public void SingletonService()
    {
        var service1 = _provider?.GetService<ISingletonService>();
        Assert.That(service1, Is.Not.Null);
        Assert.That(service1, Is.TypeOf<SingletonService>());

        var service2 = _provider?.GetService<ISingletonService>();
        Assert.That(service2, Is.Not.Null);
        Assert.That(service2, Is.SameAs(service1));
    }
}