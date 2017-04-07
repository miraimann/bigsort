using System;
using System.Threading.Tasks;

namespace Bigsort.Contracts
{
    public interface ITasksQueue
    {
        bool IsProcessing { get; }
        void Enqueue(Action action);
    }
}
