using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

namespace FileSignature
{
    static class FileReader
    {
        public static IEnumerable<Block> GetNextBlock(string filePath, long blockSize)
        {
            using (FileStream file = File.OpenRead(filePath))
            {
                long blockNumber = 0;
                byte[] buffer = new byte[blockSize];
                while (file.Read(buffer, 0, buffer.Length) >= blockSize)
                {
                    blockNumber++;
                    yield return new Block(blockNumber, buffer, blockSize);
                }
            }
        }
    }
}
