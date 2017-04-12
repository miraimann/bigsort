using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    // ReSharper disable once InconsistentNaming
    public class GrouperIOMaker
        : IGrouperIOMaker
    {
        private readonly IGrouperBuffersProviderMaker _grouperBuffersProviderMaker;
        private readonly IGroupsLinesWriterMaker _groupsLinesWriterMaker;
        private readonly IoService _ioService;
        private readonly IConfig _config;

        public GrouperIOMaker(
            IGrouperBuffersProviderMaker grouperBuffersProviderMaker, 
            IGroupsLinesWriterMaker groupsLinesWriterMaker,
            IoService ioService,
            IConfig config)
        {
            _grouperBuffersProviderMaker = grouperBuffersProviderMaker;
            _groupsLinesWriterMaker = groupsLinesWriterMaker;
            _ioService = ioService;
            _config = config;
        }

        public IGrouperIO Make(string input, string output) =>
            new IO(_grouperBuffersProviderMaker.Make(input, _config.BufferSize - 1),
                   _groupsLinesWriterMaker.Make(output));
        
        public IGrouperIO[] MakeMany(string input, string output, int count)
        {
            var result = new IGrouperIO[count];
            var length = _ioService.SizeOfFile(input);
            var blockLength = length / count;

            long offset = 0;
            using (var inputStream = _ioService.OpenRead(input))
                for (int i = 0; i < count; i++)
                {
                    long overBlock;

                    if (i == count - 1)
                        overBlock = length;
                    else
                    {
                        inputStream.Position = offset + blockLength;
                        while (inputStream.ReadByte() != Consts.EndLineByte2) ;
                        overBlock = inputStream.Position + 1;
                    }
                    
                    var readingLength = overBlock - offset;
                    result[i] = new IO(
                        _grouperBuffersProviderMaker.Make(input, _config.BufferSize - 1, offset, readingLength),
                        _groupsLinesWriterMaker.Make(output, offset));

                    offset = overBlock;
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
