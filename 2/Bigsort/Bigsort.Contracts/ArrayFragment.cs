namespace Bigsort.Contracts
{
    public class ArrayFragment<T>
    {
        public ArrayFragment(T[] array, int offset, int count)
        {
            Array = array;
            Offset = offset;
            Count = count;
        }

        public T[] Array { get; }
        public int Offset { get; }
        public int Count { get; }
    }
}
