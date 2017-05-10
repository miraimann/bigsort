using System.IO;

namespace Bigsort.Contracts.DevelopmentTools
{
    internal interface ILog
    {
        void CopyTo(Stream stream);
        string ToSring();
    }
}
