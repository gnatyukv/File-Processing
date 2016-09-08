using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Security.Cryptography;

namespace FileSignature
{
    class Block : IMultipleProducers
    {
        private byte[] data;

        public long Number { get; private set; }
        public byte[] Data
        {
            get { return data; }
            private set { value.CopyTo(data, 0); }
        }
        public string Hash { get; private set; }

        public Block(long number, byte[] data, long size)
        {
            Number = number;
            this.data = new byte[size];
            Data = data;
        }

        public void ComputeHash()
        {
            var sha256 = new SHA256Managed();
            byte[] hash = sha256.ComputeHash(Data);
            Hash = BitConverter.ToString(hash).Replace("-", String.Empty);
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Number, Hash);
        }
    }
}
