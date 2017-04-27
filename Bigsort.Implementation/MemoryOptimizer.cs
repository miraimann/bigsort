﻿using System;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class MemoryOptimizer
        : IMemoryOptimizer
    {
        private readonly ILinesReservation _linesReservation;
        private readonly IBuffersPool _buffersPool;
        private readonly IConfig _config;

        public MemoryOptimizer(
            ILinesReservation linesReservation,
            IBuffersPool buffersPool,
            IConfig config)
        {
            _linesReservation = linesReservation;
            _buffersPool = buffersPool;
            _config = config;
        }

        public void OptimizeMemoryForSort(
            int maxGroupSize, 
            int maxGroupLinesCount)
        {
            var memoryUsedForBuffers = (long)
                _buffersPool.Count *
                _config.BufferSize;

            var maxGroupRowsCount =
                (int) Math.Ceiling((double) maxGroupSize /
                                   _config.GroupRowLength);

            maxGroupSize =
                maxGroupRowsCount *
                _config.BufferSize;

            var maxSizeForGroupLines =
                _linesReservation.LineSize *
                maxGroupLinesCount;

            var maxLoadedGroupsCount =
                memoryUsedForBuffers /
                (maxGroupSize + maxSizeForGroupLines);

            var memoryForLines = (int)
                maxLoadedGroupsCount *
                maxSizeForGroupLines;

            var linesCountForReserve =
                memoryForLines /
                _linesReservation.LineSize;

            var buffersCountForFree =
                (int) Math.Ceiling((double) memoryForLines /
                                   _config.GroupRowLength);

            _buffersPool.Free(buffersCountForFree);
            _linesReservation.Load(linesCountForReserve);
        }
    }
}
