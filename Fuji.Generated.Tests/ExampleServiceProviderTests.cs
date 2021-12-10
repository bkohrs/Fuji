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

    [Test]
    public async Task TransientDisposableService_IsDisposedWhenProviderIsDisposed()
    {
        var provider = new ExampleServiceProvider();
        var disposable1 = provider.GetService<TransientDisposableService>();
        var disposable2 = provider.GetService<TransientDisposableService>();

        await provider.DisposeAsync().ConfigureAwait(false);

        Assert.That(disposable1?.IsDisposed, Is.True);
        Assert.That(disposable2?.IsDisposed, Is.True);
    }

    [Test]
    public async Task TransientAsyncDisposableService_IsDisposedWhenProviderIsDisposed()
    {
        var provider = new ExampleServiceProvider();
        var disposable1 = provider.GetService<TransientAsyncDisposableService>();
        var disposable2 = provider.GetService<TransientAsyncDisposableService>();

        await provider.DisposeAsync().ConfigureAwait(false);

        Assert.That(disposable1?.IsDisposed, Is.True);
        Assert.That(disposable2?.IsDisposed, Is.True);
    }

    [Test]
    public void ProvideSingleton_SingletonService()
    {
        var service1 = _provider?.GetService<ISingletonService>();
        Assert.That(service1, Is.Not.Null);
        Assert.That(service1, Is.TypeOf<SingletonService>());

        var service2 = _provider?.GetService<ISingletonService>();
        Assert.That(service1, Is.Not.Null);
        Assert.That(service1, Is.SameAs(service2));
    }

    [Test]
    public async Task SingletonDisposableService_IsDisposedWhenProviderIsDisposed()
    {
        var provider = new ExampleServiceProvider();
        var disposable1 = provider.GetService<SingletonDisposableService>();
        var disposable2 = provider.GetService<SingletonDisposableService>();
        Assert.That(disposable1, Is.SameAs(disposable2));

        await provider.DisposeAsync().ConfigureAwait(false);

        Assert.That(disposable1?.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public async Task SingletonAsyncDisposableService_IsDisposedWhenProviderIsDisposed()
    {
        var provider = new ExampleServiceProvider();
        var disposable1 = provider.GetService<SingletonAsyncDisposableService>();
        var disposable2 = provider.GetService<SingletonAsyncDisposableService>();
        Assert.That(disposable1, Is.SameAs(disposable2));

        await provider.DisposeAsync().ConfigureAwait(false);

        Assert.That(disposable1?.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void TransientServiceWithSingletonDependency()
    {
        var singletonService = _provider?.GetRequiredService<ISingletonService>();
        var transientService = _provider?.GetRequiredService<TransientServiceWithSingletonDependency>();
        Assert.That(transientService, Is.Not.Null);
        Assert.That(transientService?.SingletonService, Is.SameAs(singletonService));
    }
}