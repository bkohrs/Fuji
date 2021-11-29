namespace Fuji;

public interface ICodeWriterScope : IDisposable
{
    ICodeWriterScope CreateScope(string line);

    void WriteLine(string line);
}