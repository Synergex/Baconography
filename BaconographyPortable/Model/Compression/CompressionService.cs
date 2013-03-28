using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Compression
{
    public class CompressionService : ICompressionService
    {
        public unsafe byte[] Compress(byte[] bytes)
        {
            var result = new byte[LZ4n.LZ4Codec.MaximumOutputLength(bytes.Length) + 4];
            var length = LZ4n.LZ4Codec.Encode32(bytes, 0, bytes.Length, result, 4, result.Length - 4);

            if (length != result.Length)
            {
                if (length < 0)
                    throw new InvalidOperationException("Compression has been corrupted");
                var buffer = new byte[length + 4];
                Buffer.BlockCopy(result, 4, buffer, 4, length);
                fixed (byte* bytesPtr = buffer)
                {
                    *((int*)bytesPtr) = bytes.Length;
                }
                return buffer;
            }
            else if ((length + 4) == result.Length)
            {
                fixed (byte* bytesPtr = result)
                {
                    *((int*)bytesPtr) = bytes.Length;
                }
                return result;
            }
            else
                throw new InvalidOperationException("Compression has been corrupted");
        }

        public unsafe byte[] Decompress(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4)
                return new byte[0];

            int decompressedSize = 0;
            fixed (byte* bytesPtr = bytes)
            {
                decompressedSize = *((int*)bytesPtr);
            }
            return LZ4n.LZ4Codec.Decode32(bytes, 4, bytes.Length - 4, decompressedSize);
        }
    }
}
