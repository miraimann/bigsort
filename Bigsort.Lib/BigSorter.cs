namespace Bigsort.Lib
{
    public class BigSorter
    {
        public static void Sort(string input, string output) =>
            IoC.BuildSorter(input, output).Sort();
    }
}
