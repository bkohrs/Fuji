using Fuji.Generated.Services;
using JetBrains.Annotations;

namespace Fuji.Generated;

public class ServiceDependsOnLibraryService
{
    public ServiceDependsOnLibraryService([UsedImplicitly] SelfDescribedDependentLibraryService service)
    {
    }
}