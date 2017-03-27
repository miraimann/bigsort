using System;
using System.Collections.Generic;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class LinesIndexator
        : ILinesIndexator
    {
        private readonly IConfig _config;

        public LinesIndexator(IConfig config)
        {
            _config = config;
        }

        public void IndexLines(
            IEnumerable<byte> source,
            Action<long> outLinesStart,
            Action<int> outDotShift)
        {
            int i = 0, k = _config.EndLine.Length;
            long j = 0;
            
            foreach (var x in source)
            {
                if (k == _config.EndLine.Length)
                {
                    outLinesStart(j);
                    k = 0;
                    i = 0;
                }

                if (i >= 0)
                {
                    if (x == _config.Dot)
                    {
                        outDotShift(i);
                        i = -1;
                    }
                    else i++;
                }
                else if (x == _config.EndLine[k])
                     k++;
                else k = 0;

                j++;
            }
        }
    }
}
