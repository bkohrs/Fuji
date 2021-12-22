using System.Collections.Immutable;
using Fuji.Generated.Services;
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
        collection.AddTransient<CollectionProvidedService>();
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
    public void FactoryProvidedSingletonNeedingServiceProvider()
    {
        var service = _provider?.GetService<FactoryProvidedSingletonNeedingServiceProvider>();
        Assert.That(service, Is.Not.Null);
        Assert.That(service?.FactoryProvided, Is.True);
    }

    [Test]
    public void ServiceDependentOnCollectionProvided()
    {
        var service = _provider?.GetService<ServiceDependentOnCollectionProvided>();
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void MultipleImplementationService()
    {
        var service = _provider?.GetService<IMultipleImplementationService>();
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.TypeOf<MultipleImplementationService2>());

        var services = _provider?.GetServices<IMultipleImplementationService>().ToImmutableArray();
        Assert.That(services?.OfType<MultipleImplementationService1>().Count(), Is.EqualTo(1));
        Assert.That(services?.OfType<MultipleImplementationService2>().Count(), Is.EqualTo(1));
    }
}