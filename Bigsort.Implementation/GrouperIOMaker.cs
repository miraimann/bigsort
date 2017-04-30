using System;
using System.Collections.Generic;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    // ReSharper disable once InconsistentNaming
    public class GrouperIOMaker
        : IGrouperIOMaker
    {
        private readonly IInputReaderMaker _inputReaderMaker;
        private readonly IGroupsLinesWriterMaker _groupsLinesWriterMaker;
        private readonly IIoServiceMaker _ioServiceMaker;

        public GrouperIOMaker(
            IInputReaderMaker inputReaderMaker, 
            IGroupsLinesWriterMaker groupsLinesWriterMaker,
            IIoServiceMaker ioServiceMaker)
        {
            _inputReaderMaker = inputReaderMaker;
            _groupsLinesWriterMaker = groupsLinesWriterMaker;
            _ioServiceMaker = ioServiceMaker;
        }

        public IGrouperIO Make(string input, string output, IPool<byte[]> buffersPool)
        {
            var fileLength = _ioServiceMaker
                .Make(buffersPool)
                .SizeOfFile(input);
            
            return new IO(_inputReaderMaker.Make(input, fileLength, buffersPool),
                          _groupsLinesWriterMaker.Make(output, buffersPool));
        }

        public IReadOnlyList<IGrouperIO> MakeMany(
            string input, string output, int count, 
            IPool<byte[]> buffersPool)
        {
            var ioService = _ioServiceMaker.Make(buffersPool);

            var result = new List<IGrouperIO>();
            var inputFileLength = ioService.SizeOfFile(input);
            var blockLength = inputFileLength / count;

            long offset = 0;
            using (var inputStream = ioService.OpenRead(input))
                for (int i = 0; i < count; i++)
                {
                    inputStream.Position = Math.Min(
                        offset + blockLength, 
                        inputFileLength - 1);

                    while (inputStream.ReadByte() != Consts.EndLineByte2) ;
                    
                    var readingLength = inputStream.Position - offset;
                    var linesWriter = _groupsLinesWriterMaker.Make(output, buffersPool, offset);
                    var inputReader = _inputReaderMaker.Make(input, offset, readingLength, buffersPool);
                    
                    result.Add(new IO(inputReader, linesWriter));
                    if (inputStream.Position == inputFileLength)
                        break;

                    offset = inputStream.Position;
                }

            return result;
        }
        
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
