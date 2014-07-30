using System;
using System.Threading.Tasks;

namespace Kake
{
    /// <summary>
    /// Summary description for Module
    /// </summary>
    public abstract class Module
    {
        private IModuleStore _store;

        internal IModuleStore Store
        {
            set { _store = value; }
        }

        public abstract Task Configure();
    }
}