using System.Collections.Immutable;

namespace Kake
{
    public class Target
    {
        private readonly ImmutableList<Meta> _meta;
        private readonly CodeBlock _code;
        private readonly string _name;

        internal Target(string name, ImmutableList<Meta> meta, ImmutableList<Line> code)
        {
            _name = name;
            _meta = meta;
            _code = code;
        }

        public string Name
        {
            get { return _name; }
        }

        public ImmutableList<Meta> Meta
        {
            get { return _meta; }
        }

        public CodeBlock Code
        {
            get { return _code; }
        }
    }
}