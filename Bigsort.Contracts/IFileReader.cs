﻿using System;

namespace Bigsort.Contracts
{
    internal interface IFileReader
        : IDisposable
    {
        long Position { get; set; }

        long Length { get; }

        int Read(byte[] buff, int offset, int count);

        int ReadByte();
    }
}
