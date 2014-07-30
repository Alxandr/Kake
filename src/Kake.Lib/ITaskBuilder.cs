using System;
using System.Threading.Tasks;

namespace Kake
{
    /// <summary>
    /// Summary description for ITaskBuilder
    /// </summary>
    public interface ITaskBuilder
    {
        void Action(Func<Task> action);
    }
}