using Fuji.Generated.Services;
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

    [Test]
    public async Task TransientServiceWithScopedDependency()
    {
        await using var scope = _provider?.CreateAsyncScope();
        var scopedService = _provider?.GetRequiredService<IScopedService>();
        var transientService = _provider?.GetRequiredService<TransientServiceWithScopedDependency>();
        Assert.That(transientService, Is.Not.Null);
        Assert.That(transientService?.ScopedService, Is.SameAs(scopedService));
    }

    [Test]
    public void ScopedService_FromProvider()
    {
        var service1 = _provider?.GetService<IScopedService>();
        Assert.That(service1, Is.Not.Null);
        Assert.That(service1, Is.TypeOf<ScopedService>());

        var service2 = _provider?.GetService<IScopedService>();
        Assert.That(service2, Is.Not.Null);
        Assert.That(service2, Is.SameAs(service1));
    }

    [Test]
    public async Task ScopedService_WithinScope()
    {
        var rootService = _provider?.GetService<IScopedService>();
        await using var scope1 = _provider?.CreateAsyncScope();

        var service1 = scope1?.ServiceProvider.GetService<IScopedService>();
        Assert.That(service1, Is.Not.Null);
        Assert.That(service1, Is.TypeOf<ScopedService>());
        Assert.That(service1, Is.Not.SameAs(rootService));

        var service2 = scope1?.ServiceProvider.GetService<IScopedService>();
        Assert.That(service2, Is.Not.Null);
        Assert.That(service2, Is.SameAs(service1));

        await using var scope2 = _provider?.CreateAsyncScope();
        var service3 = scope2?.ServiceProvider.GetService<IScopedService>();
        Assert.That(service3, Is.Not.Null);
        Assert.That(service3, Is.Not.SameAs(service1));
    }

    [Test]
    public async Task ScopedAsyncDisposableService_FromScope()
    {
        var rootService = _provider?.GetService<ScopedAsyncDisposableService>();
        ScopedAsyncDisposableService? scopedService1;
        ScopedAsyncDisposableService? scopedService2;
        await using (var scope = _provider?.CreateAsyncScope())
        {
            scopedService1 = scope?.ServiceProvider.GetService<ScopedAsyncDisposableService>();
            Assert.That(scopedService1, Is.Not.Null);
            Assert.That(scopedService1, Is.Not.SameAs(rootService));

            scopedService2 = scope?.ServiceProvider.GetService<ScopedAsyncDisposableService>();
            Assert.That(scopedService2, Is.Not.Null);
            Assert.That(scopedService2, Is.SameAs(scopedService1));
        }

        Assert.That(scopedService1?.DisposeCount, Is.EqualTo(1));
        Assert.That(scopedService2?.DisposeCount, Is.EqualTo(1));
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

    [Test]
    public async Task SingletonService_WithinScope()
    {
        var rootSingleton = _provider?.GetService<ISingletonService>();
        await using var scope = _provider?.CreateAsyncScope();
        var service = scope?.ServiceProvider.GetService<ISingletonService>();
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.SameAs(rootSingleton));
    }

    [Test]
    public async Task DisposableSingleton_WithinScope()
    {
        SingletonAsyncDisposableService? service;
        await using (var scope = _provider?.CreateAsyncScope())
        {
            service = scope?.ServiceProvider.GetService<SingletonAsyncDisposableService>();
            Assert.That(service, Is.Not.Null);
        }
        Assert.That(service?.DisposeCount, Is.EqualTo(0));
    }

    [Test]
    public async Task ScopedServiceWithSingletonDependency()
    {
        var rootSingleton = _provider?.GetService<ISingletonService>();
        await using var scope = _provider?.CreateAsyncScope();
        var service = scope?.ServiceProvider.GetService<ScopedServiceWithSingletonDependency>();
        Assert.That(service, Is.Not.Null);
        Assert.That(service?.SingletonService, Is.SameAs(rootSingleton));
    }

    [Test]
    public async Task ScopedServiceWithTransientDependency()
    {
        await using var scope = _provider?.CreateAsyncScope();
        var scopeTransientService = _provider?.GetService<ITransientService>();
        var service = scope?.ServiceProvider.GetService<ScopedServiceWithTransientDependency>();
        Assert.That(service, Is.Not.Null);
        Assert.That(service?.TransientService, Is.Not.SameAs(scopeTransientService));
    }

    [Test]
    public void SelfDescribedTransientService()
    {
        var service1 = _provider?.GetService<SelfDescribedTransientService>();
        Assert.That(service1, Is.Not.Null);

        var service2 = _provider?.GetService<SelfDescribedTransientService>();
        Assert.That(service2, Is.Not.Null);
        Assert.That(service2, Is.Not.SameAs(service1));
    }

    [Test]
    public void SelfDescribedSingletonService()
    {
        var service1 = _provider?.GetService<SelfDescribedSingletonService>();
        Assert.That(service1, Is.Not.Null);

        var service2 = _provider?.GetService<SelfDescribedSingletonService>();
        Assert.That(service2, Is.Not.Null);
        Assert.That(service2, Is.SameAs(service1));
    }

    [Test]
    public async Task SelfDescribedScopedService()
    {
        SelfDescribedScopedService? scope1Service1;
        SelfDescribedScopedService? scope2Service1;
        await using (var scope1 = _provider?.CreateAsyncScope())
        {
            scope1Service1 = scope1?.ServiceProvider.GetService<SelfDescribedScopedService>();
            Assert.That(scope1Service1, Is.Not.Null);

            var scope1Service2 = scope1?.ServiceProvider.GetService<SelfDescribedScopedService>();
            Assert.That(scope1Service1, Is.Not.Null);
            Assert.That(scope1Service1, Is.SameAs(scope1Service2));
        }
        await using (var scope2 = _provider?.CreateAsyncScope())
        {
            scope2Service1 = scope2?.ServiceProvider.GetService<SelfDescribedScopedService>();
            Assert.That(scope2Service1, Is.Not.Null);

            var scope2Service2 = scope2?.ServiceProvider.GetService<SelfDescribedScopedService>();
            Assert.That(scope2Service1, Is.Not.Null);
            Assert.That(scope2Service1, Is.SameAs(scope2Service2));
        }
        Assert.That(scope1Service1, Is.Not.SameAs(scope2Service1));
    }

    [Test]
    public void ServiceDependsOnSelfDescribed()
    {
        var service = _provider?.GetService<ServiceDependsOnSelfDescribed>();
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void SelfProvidedService()
    {
        var service = _provider?.GetService<SelfProvidedService>();
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void DependentLibraryService()
    {
        var service = _provider?.GetService<DependentLibraryService>();
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void ServiceDependsOnLibraryService()
    {
        var service = _provider?.GetService<ServiceDependsOnLibraryService>();
        Assert.That(service, Is.Not.Null);
        var dependencyService = _provider?.GetService<SelfDescribedDependentLibraryService>();
        Assert.That(dependencyService, Is.Not.Null);
    }

    [Test]
    public void FactoryProvidedSingleton()
    {
        var service = _provider?.GetService<FactoryProvidedSingleton>();
        Assert.That(service, Is.Not.Null);
        Assert.That(service?.FactoryProvided, Is.True);
    }

    [Test]
    public void FactoryPrecedenceService()
    {
        var service = _provider?.GetService<ISelfDescribedPrecedenceService>();
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.TypeOf<CustomPrecedenceService>());
    }
}