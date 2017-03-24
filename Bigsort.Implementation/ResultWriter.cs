using System;
using System.Collections.Generic;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class ResultWriter
        : IResultWriter
    {
        private readonly IConfig _config;
        public ResultWriter(IConfig config)
        {
            _config = config;
        }

        public void Write(
            IIndexedInput input,
            IWritingStream output,
            IReadOnlyList<int> linesOrdering)
        {
            byte[] buff = new byte[_config.ResultWriterBufferSize];
            
            const int none = -1;
            int lastLine = linesOrdering.Count - 1,
                offset = 0, i = 0, line = none,
                unreadLineLength = 0;

            Action checkAndTryReleaseBuff = () =>
            {
                if (offset == buff.Length)
                {
                    output.Write(buff, 0, offset);
                    offset = 0;
                }
            };

            while (i < linesOrdering.Count)
            {
                if (line == none)
                {
                    line = linesOrdering[i];
                    var start = input.LinesStarts[line];
                    input.Bytes.Position = start;
                    unreadLineLength = (int)(input.LinesEnds[line] - start + 1);
                }

                int lengthForRead = Math.Min(buff.Length - offset, unreadLineLength),
                    count = input.Bytes.Read(buff, offset, lengthForRead);
                
                offset += count;
                checkAndTryReleaseBuff();

                var end = _config.EndLine;
                if (unreadLineLength == count)
                {
                    #region case: end line symbol(s) is(are) missing in the end of the file  
                    
                    if (line == lastLine)
                    {
                        for (int j = 0; j < end.Length; j++)
                        {
                            var k = offset - j - 1;
                            if (k < 0)
                                k = buff.Length + k;

                            if (buff[k] != end[end.Length - j - 1])
                            {
                                for (int m = 0; m < end.Length; offset++, m++)
                                {
                                    checkAndTryReleaseBuff();
                                    buff[offset] = end[m];
                                }

                                checkAndTryReleaseBuff();

                                break;
                            }
                        }
                    }

                    #endregion

                    line = none;
                    i++;
                }
                else unreadLineLength -= count;
            }

            if (offset != 0)
                output.Write(buff, 0, offset);
        }
    }
}
