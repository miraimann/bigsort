namespace Bigsort.Contracts
{
    public interface ITasksQueueMaker
    {
        ITasksQueue Make(int maxRunningTasksCount);
    }
}
