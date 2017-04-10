using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GrouperIoServiceMaker
        : IGrouperIoServiceMaker
    {
        private readonly IGrouperBuffersProviderMaker _grouperBuffersProviderMaker;
        private readonly IGroupsLinesWriterMaker _groupsLinesWriterMaker;
        private readonly IoService _ioService;
        private readonly IConfig _config;

        public GrouperIoServiceMaker(
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

        public IGrouperIoService Make(string input, string output) =>
            new Service(_grouperBuffersProviderMaker.Make(input, _config.BufferSize - 1),
                        _groupsLinesWriterMaker.Make(output));
        
        public IGrouperIoService[] MakeMany(string input, string output, int count)
        {
            var result = new IGrouperIoService[count];
            var length = _ioService.SizeOfFile(input);
            var n = length / count;

            long offset = 0, j = n;
            using (var inputStream = _ioService.OpenRead(input))
                for (int i = 0; i < count; i++)
                {
                    inputStream.Position = offset;
                    while (inputStream.ReadByte() != Consts.EndLineByte1)
                        j++;
                    
                    result[i] = new Service(
                        _grouperBuffersProviderMaker.Make(input, _config.BufferSize - 1, offset, j),
                        _groupsLinesWriterMaker.Make(output, offset));

                    offset += j;
                    j = n;
                }

            return result;
        }

        private class Service
            : IGrouperIoService
        {
            public Service(
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
