namespace Fuji.Generated;

public class SingletonDisposableService : IDisposable
{
    public int DisposeCount  { get; private set; }

    public void Dispose()
    {
        DisposeCount++;
    }
}