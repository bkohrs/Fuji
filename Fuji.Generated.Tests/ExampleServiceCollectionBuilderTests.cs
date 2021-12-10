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

    [Test]
    public void ScopedService_FromProvider()
    {
        var service1 = _provider?.GetService<ScopedAsyncDisposableService>();
        Assert.That(service1, Is.Not.Null);

        var service2 = _provider?.GetService<ScopedAsyncDisposableService>();
        Assert.That(service2, Is.Not.Null);
        Assert.That(service2, Is.SameAs(service1));
    }

    [Test]
    public async Task ScopedService_FromScope()
    {
        var rootService = _provider?.GetService<ScopedAsyncDisposableService>();
        await using var scope = _provider?.CreateAsyncScope();

        var scopedService1 = scope?.ServiceProvider.GetService<ScopedAsyncDisposableService>();
        Assert.That(scopedService1, Is.Not.Null);
        Assert.That(scopedService1, Is.Not.SameAs(rootService));

        var scopedService2 = scope?.ServiceProvider.GetService<ScopedAsyncDisposableService>();
        Assert.That(scopedService2, Is.Not.Null);
        Assert.That(scopedService2, Is.SameAs(scopedService1));
    }

    [Test]
    public async Task DisposableTransient_WithinScope()
    {
        TransientAsyncDisposableService? service;
        await using (var scope = _provider?.CreateAsyncScope())
        {
            service = scope?.ServiceProvider.GetService<TransientAsyncDisposableService>();
            Assert.That(service, Is.Not.Null);
        }
        Assert.That(service?.IsDisposed, Is.True);
    }
}