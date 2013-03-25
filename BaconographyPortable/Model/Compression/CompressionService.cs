using BaconographyPortable.Services;
using SevenZip;
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
        public byte[] Compress(byte[] bytes)
        {
            string mf = "bt4";
            Int32 dictionary = 1 << 23;
            Int32 posStateBits = 2;
            Int32 litContextBits = 2; // for normal files
            // UInt32 litContextBits = 0; // for 32-bit data
            Int32 litPosBits = 0;
            // UInt32 litPosBits = 2; // for 32-bit data
            Int32 algorithm = 2;
            Int32 numFastBytes = 128;
            CoderPropID[] propIDs = 
                {
                    CoderPropID.DictionarySize,
                    CoderPropID.PosStateBits,
                    CoderPropID.LitContextBits,
                    CoderPropID.LitPosBits,
                    CoderPropID.Algorithm,
                    CoderPropID.NumFastBytes,
                    CoderPropID.MatchFinder,
                    CoderPropID.EndMarker
                };
            object[] properties = 
                {
                    (Int32)(dictionary),
                    (Int32)(posStateBits),
                    (Int32)(litContextBits),
                    (Int32)(litPosBits),
                    (Int32)(algorithm),
                    (Int32)(numFastBytes),
                    mf,
                    true
                };
            var encoder = new SevenZip.Compression.LZMA.Encoder();
            encoder.SetCoderProperties(propIDs, properties);

            var inStream = new MemoryStream(bytes);
            var outStream = new MemoryStream();
            encoder.WriteCoderProperties(outStream);
            for (int i = 0; i < 8; i++)
                outStream.WriteByte((Byte)(bytes.Length >> (8 * i)));
            encoder.Code(inStream, outStream, bytes.Length, -1, null);
            return outStream.ToArray();
        }

        public byte[] Decompress(byte[] bytes)
        {
            SevenZip.Compression.LZMA.Decoder decoder;
            decoder = new SevenZip.Compression.LZMA.Decoder();
            var inStream = new MemoryStream(bytes);
            var outStream = new MemoryStream();
            byte[] properties = new byte[5];
            if (inStream.Read(properties, 0, 5) != 5)
                return null;
            decoder.SetDecoderProperties(properties);

            long outSize = 0;
            for (int i = 0; i < 8; i++)
            {
                int v = inStream.ReadByte();
                if (v < 0)
                    throw (new Exception("Can't Read 1"));
                outSize |= ((long)(byte)v) << (8 * i);
            }
            long compressedSize = inStream.Length - inStream.Position;


            decoder.Code(inStream, outStream, compressedSize, outSize, null);
            return outStream.ToArray();
        }
    }
}
