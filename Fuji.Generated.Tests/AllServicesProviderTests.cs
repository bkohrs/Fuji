using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Fuji.Generated.Tests;

[TestFixture]
public class AllServicesProviderTests
{
    [Test]
    public void IncludesAllServices()
    {
        var provider = new AllServicesProvider();
        var service = provider.GetService<SelfDescribedTransientService>();
        Assert.That(service, Is.Not.Null);
    }
}