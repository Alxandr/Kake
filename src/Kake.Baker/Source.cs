using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kake
{
    /// <summary>
    /// Summary description for Source
    /// </summary>
    public sealed class Source
    {
        private readonly TextReader _reader;
        private int _index;
        private Line _current;

        public Source(TextReader reader)
        {
            _reader = reader;
            _index = -1;
        }

        public async Task<bool> Advance()
        {
            _current = null;

            var line = await _reader.ReadLineAsync();
            if (line == null)
                return false;

            _current = new Line(line, ++_index);
            return true;
        }

        public Line Current
        {
            get
            {
                if (_current == null && _index < 0)
                    throw new InvalidOperationException("Advance first");

                return _current;
            }
        }
    }
}