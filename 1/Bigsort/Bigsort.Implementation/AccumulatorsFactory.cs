using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class AccumulatorsFactory
        : IAccumulatorsFactory
    {
        private readonly IIoService _ioService;
        private readonly IBytesConvertersFactory _bytesConvertersFactory;
        private readonly IPool<int[]> _arraysPool;
        private readonly IConfig _config;

        public AccumulatorsFactory(
            IPoolMaker poolMaker,
            IIoService ioService,
            IBytesConvertersFactory bytesConvertersFactory,
            IConfig config)
        {
            _arraysPool = poolMaker.Make(
                () => new int[_config.IntsAccumulatorFragmentSize]);
            _ioService = ioService;
            _bytesConvertersFactory = bytesConvertersFactory;
            _config = config;
        }

        public IAccumulator<int> CreateForInt() =>
            new Accumulator(_arraysPool, _config.IntsAccumulatorFragmentSize);

        public ICacheableAccumulator<int> CreateCacheableForInt() =>
            new CacheableAccumulator<int>(
                _bytesConvertersFactory.CreateForInt(),
                _ioService,
                _config);

        public ICacheableAccumulator<long> CreateCacheableForLong() =>
            new CacheableAccumulator<long>(
                _bytesConvertersFactory.CreateForLong(),
                _ioService,
                _config);

        private class Accumulator
            : IAccumulator<int>
        {
            private readonly int _fragmentLength;
            private readonly IPool<int[]> _arraysPool;
            private readonly IList<IPooled<int[]>> _fragments;
            private int _latestFragmentLength;

            public Accumulator(
                IPool<int[]> arraysPool,
                int fragmentLength)
            {
                _arraysPool = arraysPool;
                _fragmentLength = fragmentLength;
                _fragments = new List<IPooled<int[]>>();
            }

            public int Count =>
                Math.Max(0, _fragments.Count - 1)
                    * _fragmentLength
                    + _latestFragmentLength;

            public int this[int i] =>
                _fragments[i / _fragmentLength].Value[i % _fragmentLength];

            public void Add(int item)
            {
                if (_fragments.Count == 0 || 
                    _latestFragmentLength == _fragmentLength)
                {
                    _fragments.Add(_arraysPool.Get());
                    _latestFragmentLength = 0;
                }

                _fragments[_fragments.Count - 1]
                    .Value[_latestFragmentLength++] = item;
            }

            public IEnumerator<int> GetEnumerator()
            {
                if (_fragments.Count == 0)
                    yield break;

                for (int i = 0; i < _fragments.Count - 1; i++)
                    foreach (var item in _fragments[i].Value)
                        yield return item;

                var latestFragment = _fragments[_fragments.Count - 1];
                for (int i = 0; i < _latestFragmentLength; i++)
                        yield return latestFragment.Value[i];
            }

            IEnumerator IEnumerable.GetEnumerator() =>
                GetEnumerator();

            public void Clear()
            {
                _latestFragmentLength = 0;
                foreach (var fragment in _fragments)
                    fragment.Free();
                _fragments.Clear();
            }
        }

        private class CacheableAccumulator<T>
            : ICacheableAccumulator<T>
            where T : struct
        {
            private readonly IIoService _ioService;
            private readonly IBytesConverter<T> _bytesConverter;

            private readonly IList<string> _cacheRegister;
            private readonly IList<IReadingStream> _cache;
            private readonly IList<T> _topGeneration;

            private readonly int _itemSize, _generationLength,
                                 _enumaratingBufferSize;
            private readonly byte[] _itemBuff;

            public CacheableAccumulator(
                IBytesConverter<T> bytesConverter,
                IIoService ioService,
                IConfig config)
            {
                _bytesConverter = bytesConverter;
                _ioService = ioService;
                
                _itemSize = Marshal.SizeOf<T>();
                _generationLength = 
                    config.MaxCollectionSize / _itemSize;

                _enumaratingBufferSize = Math.Min(
                    RoundForItemSize(config.BytesEnumeratingBufferSize),
                    RoundForItemSize(config.MaxCollectionSize));

                _cacheRegister = new List<string>();
                _cache = new List<IReadingStream>();
                _topGeneration = new List<T>();
                _itemBuff = new byte[_itemSize];
            }

            public int Count =>
                _cacheRegister.Count
                    * _generationLength 
                    + _topGeneration.Count;

            public T this[int i]
            {
                get
                {
                    var fragmentIndex = i/_generationLength;
                    if (fragmentIndex == _cacheRegister.Count)
                        return _topGeneration[i%_generationLength];

                    if (_cache[fragmentIndex] == null)
                        _cache[fragmentIndex] = _ioService
                            .OpenRead(FullPathOf(_cacheRegister[fragmentIndex]));

                    var reader = _cache[fragmentIndex];
                    reader.Position = (i % _generationLength) * _itemSize;
                    reader.Read(_itemBuff, 0, _itemSize);

                    return _bytesConverter.FromBytes(_itemBuff, 0);
                }
            }

            public void Add(T item)
            {
                if (_topGeneration.Count == _generationLength)
                {
                    var cacheFragmentName = Guid.NewGuid() + ".txt";
                    _cacheRegister.Add(cacheFragmentName);

                    using (var writer = _ioService.OpenWrite(FullPathOf(cacheFragmentName)))
                        foreach (var x in _topGeneration)
                            writer.Write(_bytesConverter.ToBytes(x), 0, _itemSize);
                    
                    _cache.Add(null);
                    _topGeneration.Clear();
                }

                _topGeneration.Add(item);
            }

            public IEnumerator<T> GetEnumerator()
            {
                byte[] buff = new byte[_enumaratingBufferSize];
                for (int i = 0; i < _cache.Count; i++)
                {
                    if (_cache[i] == null)
                        _cache[i] = _ioService.OpenRead(FullPathOf(_cacheRegister[i]));

                    var reader = _cache[i];
                    long enumeratorPosition = 0;
                    int count;

                    do
                    {
                        long prevPosition = reader.Position;
                        reader.Position = enumeratorPosition;

                        count = reader.Read(buff, 0, buff.Length);

                        enumeratorPosition = reader.Position;
                        reader.Position = prevPosition;
                        
                        for (int j = 0; j < count; j += _itemSize)
                            yield return _bytesConverter.FromBytes(buff, j);

                    } while (count != 0);
                }

                foreach (var x in _topGeneration)
                    yield return x;
            }

            IEnumerator IEnumerable.GetEnumerator() =>
                GetEnumerator();

            public void Clear()
            {
                foreach (var fragment in _cache)
                    fragment?.Dispose();
                
                foreach (var fileName in _cacheRegister)
                    _ioService.DeleteFile(FullPathOf(fileName));

                _cacheRegister.Clear();
                _topGeneration.Clear();
                _cache.Clear();
            }

            public void Dispose() =>
                Clear();

            private string FullPathOf(string fileName) =>
                Path.Combine(_ioService.TempDirectory, fileName);

            private int RoundForItemSize(int length) =>
                Math.Max(length - _itemSize % _itemSize, _itemSize);
        }
    }
}
