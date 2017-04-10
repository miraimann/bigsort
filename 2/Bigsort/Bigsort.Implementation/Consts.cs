using System;

namespace Bigsort.Implementation
{
    public class Consts
    {
        public const int
            GroupPrefixMaxLettersCount = 2,
            // all combination from 0 to 2 (GroupPrefixMaxLettersCount) chars 
            // where 96 is count of chars (ASCII printable) 
            MaxGroupsCount = 96 * 96 + 96 + 1,
            TemporaryMissingResult = -1;

        public const byte
            Dot = (byte) '.',
            EndLineByte1 = (byte) '\r',
            EndLineByte2 = (byte) '\n';

        public static readonly Action ZeroAction = () => { };
    }
}
