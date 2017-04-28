using System;
using System.Collections.Generic;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    // ReSharper disable once InconsistentNaming
    public class GrouperIOMaker
        : IGrouperIOMaker
    {
        private readonly IGrouperBuffersProviderMaker _grouperBuffersProviderMaker;
        private readonly IGroupsLinesWriterMaker _groupsLinesWriterMaker;
        private readonly IIoService _ioService;
        private readonly IConfig _config;

        public GrouperIOMaker(
            IGrouperBuffersProviderMaker grouperBuffersProviderMaker, 
            IGroupsLinesWriterMaker groupsLinesWriterMaker,
            IIoService ioService,
            IConfig config)
        {
            _grouperBuffersProviderMaker = grouperBuffersProviderMaker;
            _groupsLinesWriterMaker = groupsLinesWriterMaker;
            _ioService = ioService;
            _config = config;
        }

        public IGrouperIO Make(string input, string output) =>
            new IO(_grouperBuffersProviderMaker.Make(input, _config.PhysicalBufferLength - 1),
                   _groupsLinesWriterMaker.Make(output));
        
        public IReadOnlyList<IGrouperIO> MakeMany(string input, string output, int count)
        {
            var result = new List<IGrouperIO>();
            var inputFileLength = _ioService.SizeOfFile(input);
            var blockLength = inputFileLength / count;

            long offset = 0;
            using (var inputStream = _ioService.OpenRead(input))
                for (int i = 0; i < count; i++)
                {
                    inputStream.Position = Math.Min(
                        offset + blockLength, 
                        inputFileLength - 1);

                    while (inputStream.ReadByte() != Consts.EndLineByte2);
                    
                    var readingLength = inputStream.Position - offset;
                    var linesWriter = _groupsLinesWriterMaker.Make(output, offset);
                    var buffersProvider = _grouperBuffersProviderMaker
                        .Make(input, _config.PhysicalBufferLength - 1, offset, readingLength);
                    
                    result.Add(new IO(buffersProvider, linesWriter));
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
                IGrouperBuffersProvider input, 
                IGroupsLinesWriter output)
            {
                Input = input;
                Output = output;
            }

            public IGrouperBuffersProvider Input { get; }
            public IGroupsLinesWriter Output { get; }
        }
    }
}
