using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using ObjectDB.Objects;

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
        /// Decrypted block of a fixed size (0x20) containing the header MD5 and the body MD5
        /// </summary>
        public byte[] MD5Block = new byte[] { };

        /// <summary>
        /// Header MD5 as read from MD5Block
        /// </summary>
        public byte[] HeaderMD5 = new byte[] { };

        /// <summary>
        /// Body MD5 as read from MD5Block
        /// </summary>
        public byte[] BodyMD5 = new byte[] { };

        /// <summary>
        /// Decrypted and uncompressed blob that contains the sizes of all objects in the order in which they are in the file
        /// </summary>
        public byte[] ODBObjectSizeList = new byte[] { };

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
        /// This contains the offsets to objects stored in ODBBinary
        /// </summary>
        public uint[] ODBObjectOffsets = new uint[] { };

        /// <summary>
        /// ODB embedded metadata section that describes its parent file and generation parameters 
        /// </summary>
        public string MetaInfo = "";

        private readonly int HeaderSection2Attributes;
        private readonly int HeaderType;

        private static readonly byte[] ODBMagic = new byte[] { 0x52, 0x90, 0xD4, 0x30, 0x67, 0x14, 0x7E, 0x47, 0x81, 0xF2, 0x3C, 0x4B, 0x73, 0xF0, 0xF7, 0x37 };

        private const int HashblockSize = 0x20;
        private const int ExpectedHeaderSize = 0x44;

        /// <summary>
        /// Creates an instance of an ObjectDB file
        /// </summary>
        /// <param name="fileBytes">ObjectDB file as an array of bytes</param>
        public ODBFile(byte[] fileBytes)
        {
            // Magic (0x10) + header (0x44) + hashblock (0x20) is the bare minimum for a linear ObjectDB file
            if (fileBytes.Length < (ODBMagic.Length + ExpectedHeaderSize + HashblockSize))
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

                int headerSize = reader.ReadInt32();

                if (headerSize != ExpectedHeaderSize)
                {
                    Console.WriteLine("Warning: Incompatible ODB header (invalid size)");
                    return;
                }

                int HeaderODBType = reader.ReadInt32(); // Typically 00010704 in SMR-D and SMR-F
                int HeaderFileHashBlockOffset = reader.ReadInt32();
                int HeaderClientID = reader.ReadInt32();
                int HeaderXorMaskSize = reader.ReadInt32();

                int HeaderMetaInfoBlockSize = reader.ReadInt32();
                int headerUnkConstant0 = reader.ReadInt32();
                int headerOffsetArraySize = reader.ReadInt32();

                // Describes the 3 data sections
                int HeaderOdbSection1Size = reader.ReadInt32();
                int HeaderOdbSection1Attributes = reader.ReadInt32();
                int HeaderOdbSection2Size = reader.ReadInt32();
                int HeaderOdbSection2Attributes = reader.ReadInt32();
                int HeaderOdbSection3Size = reader.ReadInt32();
                int HeaderOdbSection3Attributes = reader.ReadInt32();

                HeaderSection2Attributes = HeaderOdbSection2Attributes;

#if DEBUG
                Console.WriteLine($"File size: {fileBytes.Length:X8} ({fileBytes.Length}), Header size: {headerSize:X8}");
                Console.WriteLine($"HeaderODBType: {HeaderODBType:X8}");
                Console.WriteLine($"HeaderFileHashBlockOffset: {HeaderFileHashBlockOffset:X8}");
                Console.WriteLine($"HeaderClientID: {HeaderClientID:X8}");
                Console.WriteLine($"HeaderFileHashBlockMaskSize: {HeaderXorMaskSize:X8}");
                Console.WriteLine($"HeaderMetaInfoBlockSize: {HeaderMetaInfoBlockSize:X8}");
                Console.WriteLine($"headerUnkConstant0: {headerUnkConstant0:X8}");
                Console.WriteLine($"headerValueOffsetArraySize: {headerOffsetArraySize:X8}");
                Console.WriteLine($"Section 1 size: {HeaderOdbSection1Size:X8}");
                Console.WriteLine($"Section 1 attributes: {HeaderOdbSection1Attributes:X8}");
                Console.WriteLine($"Section 2 size: {HeaderOdbSection2Size:X8}");
                Console.WriteLine($"Section 2 attributes: {HeaderOdbSection2Attributes:X8}");
                Console.WriteLine($"Section 3 size: {HeaderOdbSection3Size:X8}");
                Console.WriteLine($"Section 3 attributes: {HeaderOdbSection3Attributes:X8}");
#endif
                // Uninitialized data
                int Constant1 = reader.ReadInt32(); // ee ee ee ee
                int Constant2 = reader.ReadInt32(); // ee ee ee ee

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

                // Create our own MD5 hash for verification
                // Header hash = md5(magic + header + metainfo)
                byte[] expectedHeaderMd5 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                byte[] expectedBodyMd5 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                int headerMd5EndPosition = 0;
                using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    headerMd5EndPosition = (int)reader.BaseStream.Position;
                    expectedHeaderMd5 = md5.ComputeHash(fileBytes.Take(headerMd5EndPosition).ToArray());
                }

                // Read out MD5 block containing expected hashes of the header and body
                MD5Block = CreateDataSection(reader, HashblockSize, 0, xorMask, null);
                HeaderMD5 = MD5Block.Take(0x10).ToArray();
                BodyMD5 = MD5Block.Skip(0x10).Take(0x10).ToArray();

                // Create the 3x primary data sections
                ODBObjectSizeList = CreateDataSection(reader, HeaderOdbSection1Size, HeaderOdbSection1Attributes, xorMask, bf);
                ODBBinary = CreateDataSection(reader, HeaderOdbSection2Size, HeaderOdbSection2Attributes, xorMask, bf);
                ODBStrings = CreateDataSection(reader, HeaderOdbSection3Size, HeaderOdbSection3Attributes, xorMask, bf);

                // Create our own body MD5 hash : md5( xor(section1) +  xor(section2) +  xor(section3) )
                using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    int bodyOffset = headerMd5EndPosition + 0x20;
                    int bodySize = (int)reader.BaseStream.Position - bodyOffset;
                    byte[] bodyBytes = fileBytes.Skip(bodyOffset).Take(bodySize).ToArray();
                    bodyBytes = XorTransform(bodyBytes, bodyOffset, xorMask);
                    expectedBodyMd5 = md5.ComputeHash(bodyBytes);
                }

                // Read out flash data if it is available (SMR-F)
                ODBFlashBinary = CreateDataSection(reader, HeaderFlashSize, 0, xorMask, null);

                ODBObjectOffsets = TransformOffsetSection(ODBObjectSizeList, headerOffsetArraySize, HeaderOdbSection1Attributes);
#if DEBUG
                Console.WriteLine($"Blowfish Key: {BitUtility.BytesToHex(decryptionKey)}");
                Console.WriteLine($"Header MD5: Read: {BitUtility.BytesToHex(HeaderMD5)} Calculated: {BitUtility.BytesToHex(expectedHeaderMd5)}");
                Console.WriteLine($"Body MD5:   Read: {BitUtility.BytesToHex(BodyMD5)} Calculated: {BitUtility.BytesToHex(expectedBodyMd5)}");
                Console.WriteLine($"Read complete - cursor at {reader.BaseStream.Position}, file size: {fileBytes.Length}");
#endif
                if (reader.BaseStream.Position != fileBytes.Length)
                {
                    Console.WriteLine("Warning: some bytes may have been skipped (cursor does not stop at file end)");
                }
                if (!HeaderMD5.SequenceEqual(expectedHeaderMd5))
                {
                    Console.WriteLine($"Header MD5 Mismatch: Expected: {BitUtility.BytesToHex(HeaderMD5)} Calculated: {BitUtility.BytesToHex(expectedHeaderMd5)}");
                }
                if (!BodyMD5.SequenceEqual(expectedBodyMd5))
                {
                    Console.WriteLine($"Body MD5 Mismatch:   Expected: {BitUtility.BytesToHex(BodyMD5)} Calculated: {BitUtility.BytesToHex(expectedBodyMd5)}");
                }

                HeaderType = HeaderODBType; 
            }
            FileBytes = fileBytes;
            // NOTE: Not sure if the ODB file is capable of specifying an encoding, defaulting to utf8
            ODBStringTable = ReadODBValueTable(ODBStrings, Encoding.UTF8);
#if DEBUG
            for (int i = 0; i < ODBObjectOffsets.Length; i++)
            {
                ODBObject obj = GetObjectAt(i);
                Console.WriteLine($"found object type: {obj}");
            }
#endif
        }

        private byte[] CreateDataSection(BinaryReader reader, int sectionSize, int sectionAttributes, byte[] xorMask, BlowFish bf)
        {
            int cursorPosition = (int)reader.BaseStream.Position;
            byte[] sectionRawBytes = reader.ReadBytes(sectionSize);
            // If no blowfish instance is provided, only perform xor transform
            if (bf is null)
            {
                return XorTransform(sectionRawBytes, cursorPosition, xorMask);
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

        private static uint[] TransformOffsetSection(byte[] section1Buf, int offset_array_size, int optimization_level)
        {
            uint[] s1buf = new uint[offset_array_size];

            using (ODBReader reader = new ODBReader(new BinaryReader(new MemoryStream(section1Buf)), optimization_level))
            {
                uint prev = 0;
                for (int i = 0; i < offset_array_size; i++)
                {
                    s1buf[i] = prev;

                    prev += reader.ReadUint32();
                }
                //s1buf[offset_array_size] = prev;
            }

            return s1buf;
        }

        public ODBObject GetObjectAt(int index)
        {
            uint offset = ODBObjectOffsets[index];
#if DEBUG
            Console.WriteLine($"object at offset {offset}");
#endif

            BinaryReader breader = new BinaryReader(new MemoryStream(ODBBinary.Skip((int)offset).ToArray()));
            ODBReader odbreader = new ODBReader(breader, HeaderSection2Attributes, ODBStrings);
            using (odbreader)
            {
                int object_type = odbreader.ReadInt32();
                ODBObject obj = CreateObjectById(object_type);

                if (obj == null)
                {
                    return null;
                }

                obj.ODBType = HeaderType;

                // TODO: should probably implement some sort of object cache, or just parse
                // all objects in the constructor. Re-parsing the object every time it's needed
                // is ineffiecent. that said, it might also be a good idea to just leave the
                // caching to the caller and keep this function as-is.
                obj.ParseFromReader(odbreader);

                return obj;
            }
        }

        private static ODBObject CreateObjectById(int id)
        {
            switch (id)
            {
                case 0x32:
                    return new VDXFlashImpl();
                case 0x71:
                    return new FlashDataImpl();
                default:
                    return new UnimplementedObject(id);
            }
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
