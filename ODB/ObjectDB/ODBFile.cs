using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace ObjectDB
{
    /// <summary>
    /// ODBFile
    /// Represents a linear ObjectDB file. May be created with an array of bytes, or from a file via FromFile()
    /// </summary>
    public class ODBFile
    {
        /// <summary>
        /// Byte array of the input ODB file
        /// </summary>
        public byte[] FileBytes = new byte[] { };

        /// <summary>
        /// Decrypted block of a fixed size (0x20) that seems to be a hash
        /// </summary>
        public byte[] HashBlock = new byte[] { };

        /// <summary>
        /// Decrypted and uncompressed blob of unknown content
        /// </summary>
        public byte[] ODBUnknown = new byte[] { };

        /// <summary>
        /// Decrypted and uncompressed blob that contains ODB binary content (and maybe the file table too)
        /// </summary>
        public byte[] ODBBinary = new byte[] { };

        /// <summary>
        /// Decrypted and uncompressed blob that contains the ODB strings table
        /// </summary>
        public byte[] ODBStrings = new byte[] { };

        /// <summary>
        /// Individual string records that are decoded from the Strings section
        /// </summary>
        public string[] ODBStringTable = new string[] { };

        /// <summary>
        /// Decrypted blob that contains binary flash data (SMR-F)
        /// </summary>
        public byte[] ODBFlashBinary = new byte[] { };

        /// <summary>
        /// ODB embedded metadata section that describes its parent file and generation parameters 
        /// </summary>
        public string MetaInfo = "";

        private static readonly byte[] ODBMagic = new byte[] { 0x52, 0x90, 0xD4, 0x30, 0x67, 0x14, 0x7E, 0x47, 0x81, 0xF2, 0x3C, 0x4B, 0x73, 0xF0, 0xF7, 0x37 };

        /// <summary>
        /// Creates an instance of an ObjectDB file
        /// </summary>
        /// <param name="fileBytes">ObjectDB file as an array of bytes</param>
        public ODBFile(byte[] fileBytes)
        {
            // Magic (0x10) + header (0x44) is the bare minimum for a linear ObjectDB file
            if (fileBytes.Length < 0x54)
            {
                Console.WriteLine("Warning: Unrecognized ODB file (invalid size)");
                return;
            }

            using (BinaryReader reader = new BinaryReader(new MemoryStream(fileBytes)))
            {
                byte[] fileMagicBytes = reader.ReadBytes(0x10);
                if (!fileMagicBytes.SequenceEqual(ODBMagic))
                {
                    Console.WriteLine("Warning: Incompatible ODB file (unrecognized magic)");
                    return;
                }

                const int expectedHeaderSize = 0x44;
                int headerSize = reader.ReadInt32();

                if (headerSize != expectedHeaderSize)
                {
                    Console.WriteLine("Warning: Incompatible ODB header (invalid size)");
                    return;
                }

                int HeaderODBType = reader.ReadInt32(); // Typically 00010704 in SMR-D and SMR-F
                int HeaderFileHashBlockOffset = reader.ReadInt32();
                int HeaderClientID = reader.ReadInt32();
                int HeaderXorMaskSize = reader.ReadInt32();

                int headerValue5 = reader.ReadInt32();
                int headerValue6 = reader.ReadInt32();
                int headerValue7 = reader.ReadInt32();

                // Describes the 3 data sections
                int HeaderOdbSection1Size = reader.ReadInt32();
                int HeaderOdbSection1Attributes = reader.ReadInt32();
                int HeaderOdbSection2Size = reader.ReadInt32();
                int HeaderOdbSection2Attributes = reader.ReadInt32();
                int HeaderOdbSection3Size = reader.ReadInt32();
                int HeaderOdbSection3Attributes = reader.ReadInt32();

#if DEBUG
                Console.WriteLine($"File size: {fileBytes.Length:X8} ({fileBytes.Length}), Header size: {headerSize:X8}");
                Console.WriteLine($"HeaderODBType: {HeaderODBType:X8}");
                Console.WriteLine($"HeaderFileHashBlockOffset: {HeaderFileHashBlockOffset:X8}");
                Console.WriteLine($"HeaderClientID: {HeaderClientID:X8}");
                Console.WriteLine($"HeaderFileHashBlockMaskSize: {HeaderXorMaskSize:X8}");
                Console.WriteLine($"headerValue5: {headerValue5:X8}");
                Console.WriteLine($"headerValue6: {headerValue6:X8}");
                Console.WriteLine($"headerValue7: {headerValue7:X8}");
                Console.WriteLine($"Section 1 size: {HeaderOdbSection1Size:X8}");
                Console.WriteLine($"Section 1 attributes: {HeaderOdbSection1Attributes:X8}");
                Console.WriteLine($"Section 2 size: {HeaderOdbSection2Size:X8}");
                Console.WriteLine($"Section 2 attributes: {HeaderOdbSection2Attributes:X8}");
                Console.WriteLine($"Section 3 size: {HeaderOdbSection3Size:X8}");
                Console.WriteLine($"Section 3 attributes: {HeaderOdbSection3Attributes:X8}");
#endif
                // Unknown, 8 byte array?
                int headerValue14 = reader.ReadInt32(); // ee ee ee ee
                int headerValue15 = reader.ReadInt32(); // ee ee ee ee

                // Flash payload size
                int HeaderFlashSize = reader.ReadInt32();

                // MetaInfo size in bytes
                int HeaderMetaInfoSize = reader.ReadInt32();

                // Header is done, entering MetaInfo section
                MetaInfo = Encoding.ASCII.GetString(reader.ReadBytes(HeaderMetaInfoSize));

                // MetaInfo done, entering blocks of xor'd data

                // Initialize xor mask
                byte[] xorMask = CreateXorMask(HeaderXorMaskSize);
                byte[] decryptionKey = BlowfishKeyTable.GetBlowfishKeyFromClientID(HeaderClientID);
                BlowFish bf = new BlowFish(decryptionKey);

                HashBlock = CreateDataSection(reader, 0x20, 0, xorMask, null, true);
                ODBUnknown = CreateDataSection(reader, HeaderOdbSection1Size, HeaderOdbSection1Attributes, xorMask, bf);
                ODBBinary = CreateDataSection(reader, HeaderOdbSection2Size, HeaderOdbSection2Attributes, xorMask, bf);
                ODBStrings = CreateDataSection(reader, HeaderOdbSection3Size, HeaderOdbSection3Attributes, xorMask, bf);
                ODBFlashBinary = CreateDataSection(reader, HeaderFlashSize, 0, xorMask, null);

#if DEBUG
                Console.WriteLine($"Blowfish Key: {BitUtility.BytesToHex(decryptionKey)}");
                Console.WriteLine($"Read complete - cursor at {reader.BaseStream.Position}, file size: {fileBytes.Length}");
#endif
                if (reader.BaseStream.Position != fileBytes.Length)
                {
                    Console.WriteLine("Warning: some bytes may have been skipped (cursor does not stop at file end)");
                }
            }
            FileBytes = fileBytes;
            // NOTE: ASCII may not be the best option here, since the string table actually specifies their required encoding (e.g. infer from de-DE)
            ODBStringTable = ReadODBValueTable(ODBStrings, Encoding.ASCII);
        }

        private byte[] CreateDataSection(BinaryReader reader, int sectionSize, int sectionAttributes, byte[] xorMask, BlowFish bf, bool invertedCursorBehavior=false)
        {
            int cursorPosition = (int)reader.BaseStream.Position;
            byte[] sectionRawBytes = reader.ReadBytes(sectionSize);
            // If no blowfish instance is provided, only perform xor transform
            // The hashblock has a quirk that requires the cursor to be set at the end of the block
            if (bf is null)
            {
                return XorTransform(sectionRawBytes, invertedCursorBehavior ? (int)reader.BaseStream.Position : cursorPosition, xorMask);
            }
            byte[] sectionUnxorBytes = XorTransform(sectionRawBytes, cursorPosition, xorMask);
            byte[] sectionPlainBytes = bf.Decrypt_ECB(sectionUnxorBytes);
            if ((sectionAttributes & 0x100) > 0)
            {
                sectionPlainBytes = Inflate(sectionPlainBytes);
            }
            return sectionPlainBytes;
        }

        private static string[] ReadODBValueTable(byte[] odbIndexBytes, Encoding encoding)
        {
            List<string> entries = new List<string>();

            int lastPosition = 0;
            for (int i = 0; i < odbIndexBytes.Length; i++)
            {
                if (odbIndexBytes[i] != 0)
                {
                    continue;
                }
                int stringSize = i - lastPosition;
                byte[] stringData = new byte[stringSize + 1];
                Array.ConstrainedCopy(odbIndexBytes, lastPosition, stringData, 0, stringSize);
                lastPosition = i;
                string outputString = encoding.GetString(stringData);
#if DEBUG
                /*
                // even in debug, this is a bit too verbose
                Console.WriteLine($"[Position {entries.Count} ({entries.Count:X}) @ offset {i} ({i:X})]");
                Console.WriteLine(outputString);
                */
#endif
                entries.Add(outputString);
            }
            return entries.ToArray();
        }

        /// <summary>
        /// Creates an ODBFile instance from a file on disk
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static ODBFile FromFile(string filePath)
        {
            return new ODBFile(File.ReadAllBytes(filePath));
        }

        private static byte[] XorTransform(byte[] targetBytes, int offset, byte[] xorMask)
        {
            List<byte> result = new List<byte>(targetBytes);
            int endOffset = offset + targetBytes.Length;
            for (int i = offset; i < endOffset; i++)
            {
                int rebasedOffset = i - offset;
                result[rebasedOffset] = (byte)(xorMask[i % xorMask.Length] ^ targetBytes[rebasedOffset]);
            }
            return result.ToArray();
        }

        private static byte[] CreateXorMask(int maskSize)
        {
            byte[] result = new byte[maskSize];
            UInt32 state = (UInt32)maskSize;
            for (int i = 0; i < maskSize; i++)
            {
                unchecked
                {
                    state = 0x41C64E6D * state + 0x3039;
                }
                result[i] = (byte)((state >> 16) & 0xFF);
            }
            return result;
        }

        private static byte[] Inflate(byte[] input)
        {
            using (MemoryStream ms = new MemoryStream(input))
            {
                MemoryStream msInner = new MemoryStream();

                // Read past the zlib header bytes (proprietary dword, zlib word)
                BinaryReader br = new BinaryReader(ms);
                int expectedSize = br.ReadInt32();
                short zlibHeader = br.ReadInt16();

                using (DeflateStream z = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    z.CopyTo(msInner);
                }
                byte[] result = msInner.ToArray();

#if DEBUG
                Console.WriteLine($"Inflate - Expected size: {expectedSize}, Actual size: {result.Length}, Zlib Header: {zlibHeader:X4}");
#endif
                if (expectedSize != result.Length)
                {
                    throw new NotImplementedException("Inflated output length does not match header");
                }
                return result;
            }
        }

    }
}
