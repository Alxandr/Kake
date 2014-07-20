using System.Collections.Immutable;

namespace Kake
{
    public class Meta
    {
        private readonly ImmutableArray<string> _args;
        private readonly string _name;
        private int _index;

        public Meta(string name, string[] args, int index)
        {
            _name = name;
            _args = ImmutableArray.Create(args);
            _index = index;
        }

        public string Name
        {
            get { return _name; }
        }

        public ImmutableArray<string> Args
        {
            get { return _args; }
        }
    }
}