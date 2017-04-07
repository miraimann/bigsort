namespace Bigsort.Contracts
{
    public interface ITasksQueueMaker
    {
        ITasksQueue MakeQueue(int maxThreadsCount);

        IPriorityTasksQueue MakePriorityQueue(int maxThreadsCount);
    }
}
