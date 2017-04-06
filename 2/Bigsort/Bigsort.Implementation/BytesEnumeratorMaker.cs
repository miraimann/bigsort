using System;
using System.Collections;
using System.Collections.Generic;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class BytesEnumeratorMaker
        : IBytesEnumeratorMaker

    {
        private readonly Func<byte[], IEnumerator<byte>> _make;

        public BytesEnumeratorMaker(IConfig config)
        {
            if (config.IsLittleEndian)
                 _make = MakeLittleEndian;
            else _make = MakeBigEndian;
        }

        public IEnumerator<byte> MakeFor(byte[] array) =>
            _make(array);

        private IEnumerator<byte> MakeBigEndian(byte[] array)
        {
            int i = 0;
            var segmentsLength = array.Length - array.Length % sizeof(ulong);
            for (; i < segmentsLength; i += sizeof(ulong))
            {
                var segment = BitConverter.ToUInt64(array, i);
                yield return (byte)((segment & 0xFF00000000000000) >> 56);
                yield return (byte)((segment & 0x00FF000000000000) >> 48);
                yield return (byte)((segment & 0x0000FF0000000000) >> 40);
                yield return (byte)((segment & 0x000000FF00000000) >> 32);
                yield return (byte)((segment & 0x00000000FF000000) >> 24);
                yield return (byte)((segment & 0x0000000000FF0000) >> 16);
                yield return (byte)((segment & 0x000000000000FF00) >> 8);
                yield return (byte) (segment & 0x00000000000000FF);
            }

            for (; i < array.Length; i++)
                yield return array[i];
        }

        private IEnumerator<byte> MakeLittleEndian(byte[] array)
        {
            int i = 0;
            var segmentsLength = array.Length - array.Length % sizeof(ulong);
            for (; i < segmentsLength; i += sizeof(ulong))
            {
                var segment = BitConverter.ToUInt64(array, i);
                yield return (byte) (segment & 0x00000000000000FF);
                yield return (byte)((segment & 0x000000000000FF00) >> 8);
                yield return (byte)((segment & 0x0000000000FF0000) >> 16);
                yield return (byte)((segment & 0x00000000FF000000) >> 24);
                yield return (byte)((segment & 0x000000FF00000000) >> 32);
                yield return (byte)((segment & 0x0000FF0000000000) >> 40);
                yield return (byte)((segment & 0x00FF000000000000) >> 48);
                yield return (byte)((segment & 0xFF00000000000000) >> 56);
            }

            for (; i < array.Length; i++)
                yield return array[i];
        }

        //private struct LittleEndianEnumerator
        //    : IEnumerator<byte>
        //{
        //    private readonly byte[] _source;
        //    private int i = 

        //    public LittleEndianEnumerator(byte[] source)
        //    {
        //        _source = source;

        //    }

        //    public bool MoveNext()
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public byte Current { get; }

        //    object IEnumerator.Current => 
        //        Current;

        //    public void Reset()
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public void Dispose() { }
        //}
    }
}
