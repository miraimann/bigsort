namespace Bigsort.Contracts
{
    public interface IBuffersReaderMaker
    {
        IBuffersReader Make(string path, int buffLength,
            ITasksQueue tasksQueue);
    }
}
