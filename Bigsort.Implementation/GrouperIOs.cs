﻿using System;
using System.Collections;
using System.Collections.Generic;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class GrouperIOs
        : IGrouperIOs
    {
        private readonly Lazy<IReadOnlyList<IGrouperIO>> _implementation;

        public GrouperIOs(
            IInputReaderFactory inputReaderFactory, 
            IGroupsLinesOutputFactory groupsLinesOutputFactory,
            IIoService ioService, 
            IConfig config)
        {
            _implementation = new Lazy<IReadOnlyList<IGrouperIO>>(() =>
            {
                var inputFileLength = ioService.SizeOfFile(config.InputFilePath);
                if (config.GrouperEnginesCount == 1)
                    return new[]
                    {
                        new IO(inputReaderFactory.Create(inputFileLength),
                               groupsLinesOutputFactory.Create())
                    };
                
                var implementation = new List<IGrouperIO>();
                var blockLength = inputFileLength/config.GrouperEnginesCount;

                long offset = 0;
                using (var inputStream = ioService.OpenRead(config.InputFilePath))
                    for (int i = 0; i < config.GrouperEnginesCount; i++)
                    {
                        inputStream.Position = Math.Min(
                            offset + blockLength,
                            inputFileLength - 1);

                        while (inputStream.ReadByte() != Consts.EndLineByte2) ;

                        var readingLength = inputStream.Position - offset;

                        implementation.Add(new IO(
                            inputReaderFactory.Create(offset, readingLength),
                            groupsLinesOutputFactory.Create(offset)));


                        if (inputStream.Position == inputFileLength)
                            break;

                        offset = inputStream.Position;
                    }

                return implementation;
            });
        }

        public int Count =>
            _implementation.Value.Count;

        public IGrouperIO this[int i] =>
            _implementation.Value[i];

        public IEnumerator<IGrouperIO> GetEnumerator() =>
            _implementation.Value.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        private class IO
            : IGrouperIO
        {
            public IO(
                IInputReader input, 
                IGroupsLinesOutput output)
            {
                Input = input;
                Output = output;
            }

            public IInputReader Input { get; }
            public IGroupsLinesOutput Output { get; }
        }
    }
}
