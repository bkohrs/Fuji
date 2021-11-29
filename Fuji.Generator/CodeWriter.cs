using System.Text;

namespace Fuji;

public class CodeWriter
{
    private readonly StringBuilder _builder;

    public CodeWriter()
    {
        _builder = new();
    }

    public ICodeWriterScope CreateScope(string line)
    {
        return new CodeWriterScope(WriteLine, line);
    }

    public override string ToString()
    {
        return _builder.ToString();
    }

    public void WriteLine(string line)
    {
        _builder.AppendLine(line);
    }

    private class CodeWriterScope : ICodeWriterScope
    {
        private readonly Action<string> _writeLine;

        public CodeWriterScope(Action<string> writeLine, string line)
        {
            _writeLine = writeLine;
            if (!string.IsNullOrWhiteSpace(line))
                _writeLine(line);
            _writeLine("{");
        }

        public void Dispose()
        {
            _writeLine("}");
        }

        public ICodeWriterScope CreateScope(string line)
        {
            return new CodeWriterScope(WriteLine, line);
        }

        public void WriteLine(string line)
        {
            _writeLine($"    {line}");
        }
    }
}