using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Fuji.Generated.Tests;

[TestFixture]
public class ExampleServiceProviderTests
{
    private IServiceProvider? _provider;

    [SetUp]
    public void SetUp()
    {
        _provider = new ExampleServiceProvider();
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
    public void TransientServiceWithTransientDependency()
    {
        var service1 = _provider?.GetService<TransientServiceWithTransientDependency>();
        Assert.That(service1, Is.Not.Null);

        var service2 = _provider?.GetService<TransientServiceWithTransientDependency>();
        Assert.That(service2, Is.Not.Null);
        Assert.That(service2, Is.Not.SameAs(service1));
    }
}