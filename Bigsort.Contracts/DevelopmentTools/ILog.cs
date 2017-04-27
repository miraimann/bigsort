using System.IO;

namespace Bigsort.Contracts.DevelopmentTools
{
    public interface ILog
    {
        void CopyTo(Stream stream);
        string ToSring();
    }
}
