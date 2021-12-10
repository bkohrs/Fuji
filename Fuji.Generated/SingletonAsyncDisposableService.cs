namespace Fuji.Generated;

public class SingletonAsyncDisposableService : IAsyncDisposable
{
    public int DisposeCount { get; private set; }

    public ValueTask DisposeAsync()
    {
        DisposeCount++;
        return default;
    }
}