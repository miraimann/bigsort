using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bigsort.Contracts
{
    public interface IBytesEnumeratorMaker
    {
        IEnumerator<byte> MakeFor(byte[] array);
    }
}
