namespace Fuji.Generated;

public class ScopedDisposableService : IDisposable
{
    public int DisposeCount { get; private set; }

    public void Dispose()
    {
        DisposeCount++;
    }
}