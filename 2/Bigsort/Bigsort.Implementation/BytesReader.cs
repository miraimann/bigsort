using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bigsort.Implementation
{
    public unsafe struct BytesReader
    {
        private readonly byte* _source;
        private ulong _segment;
        private int _segmentOver;

        public BytesReader(byte[] source)
        {
            _segmentOver = sizeof(ulong);
            fixed (byte* sourcePtr = source)
            {
                _source = sourcePtr;
                ulong* segmentPtr = (ulong*)_source;
                _segment = *segmentPtr;
            }
        }

        public byte GetByte(int i)
        {
            if (i == _segmentOver)
            {
                ulong* segmentPtr = (ulong*)(_source + _segmentOver);
                _segment = *segmentPtr;
                _segmentOver += sizeof(ulong);
            }

            switch (i % sizeof(ulong))
            {
                case 0: return (byte)(_segment & 0x00000000000000FF);
                case 1: return (byte)((_segment & 0x000000000000FF00) >> 8);
                case 2: return (byte)((_segment & 0x0000000000FF0000) >> 16);
                case 3: return (byte)((_segment & 0x00000000FF000000) >> 24);
                case 4: return (byte)((_segment & 0x000000FF00000000) >> 32);
                case 5: return (byte)((_segment & 0x0000FF0000000000) >> 40);
                case 6: return (byte)((_segment & 0x00FF000000000000) >> 48);
                case 7: return (byte)((_segment & 0xFF00000000000000) >> 56);
            }

            return 0; // impossible
        }
    }
}
