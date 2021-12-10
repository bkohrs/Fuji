namespace Fuji.Generated;

public class ScopedAsyncDisposableService : IAsyncDisposable
{
    public int DisposeCount { get; private set; }

    public ValueTask DisposeAsync()
    {
        DisposeCount++;
        return default;
    }
}