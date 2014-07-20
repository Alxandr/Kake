using System.Collections.Immutable;

namespace Kake
{
    public class KakeUnit
    {
        private CodeBlock _code;
        private ImmutableList<Meta> _metas;
        private ImmutableList<Target> _targets;

        internal KakeUnit(ImmutableList<Meta> metas, ImmutableList<Line> code, ImmutableList<Target> targets)
        {
            _metas = metas;
            _code = code;
            _targets = targets;
        }

        public CodeBlock Code
        {
            get { return _code; }
        }

        public ImmutableList<Meta> Meta
        {
            get { return _metas; }
        }

        public ImmutableList<Target> Targets
        {
            get { return _targets; }
        }
    }
}