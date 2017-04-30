using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    // ReSharper disable once InconsistentNaming
    public class GrouperIOs
        : IGrouperIOs
    {
        private readonly IReadOnlyList<IGrouperIO> _implementation;

        public GrouperIOs(
            string inputFilePath, 
            IInputReaderMaker inputReaderMaker, 
            IGroupsLinesWriterFactory groupsLinesWriterFactory,
            IIoService ioService, 
            IConfig config)
        {
            var inputFileLength = ioService.SizeOfFile(inputFilePath);
            if (config.GrouperEnginesCount == 1)
                _implementation = new[]
                {
                    new IO(inputReaderMaker.Make(inputFileLength),
                           groupsLinesWriterFactory.Create())
                };
            else
            {
                var implementation = new List<IGrouperIO>();
                var blockLength = inputFileLength/config.GrouperEnginesCount;

                long offset = 0;
                using (var inputStream = ioService.OpenRead(inputFilePath))
                    for (int i = 0; i < config.GrouperEnginesCount; i++)
                    {
                        inputStream.Position = Math.Min(
                            offset + blockLength,
                            inputFileLength - 1);

                        while (inputStream.ReadByte() != Consts.EndLineByte2) ;

                        var readingLength = inputStream.Position - offset;

                        implementation.Add(new IO(
                            inputReaderMaker.Make(offset, readingLength),
                            groupsLinesWriterFactory.Create(offset)));


                        if (inputStream.Position == inputFileLength)
                            break;

                        offset = inputStream.Position;
                    }

                _implementation = implementation;
            }
        }

        public int Count =>
            _implementation.Count;

        public IGrouperIO this[int i] =>
            _implementation[i];

        public IEnumerator<IGrouperIO> GetEnumerator() =>
            _implementation.Cast<IGrouperIO>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            _implementation.GetEnumerator();
        
        // ReSharper disable once InconsistentNaming
        private class IO
            : IGrouperIO
        {
            public IO(
                IInputReader input, 
                IGroupsLinesWriter output)
            {
                Input = input;
                Output = output;
            }

            public IInputReader Input { get; }
            public IGroupsLinesWriter Output { get; }
        }
    }
}
