using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ObjectDB
{
    internal class ODBReader : IDisposable
    {
        public readonly int OptimizationLevel;

        private readonly BinaryReader Reader;
        private readonly byte[] StringsBase;

        public ODBReader(BinaryReader input, int optimizationLevel)
        {
            Reader = input;
            OptimizationLevel = optimizationLevel;
        }

        public ODBReader(BinaryReader input, int optimizationLevel, byte[] stringsBase)
        {
            Reader = input;
            OptimizationLevel = optimizationLevel;
            StringsBase = stringsBase;
        }

        public uint ReadUint32()
        {
            if ((OptimizationLevel & 0x20) == 0)
            {
                return BitConverter.ToUInt32(ReadBytesEndiannessSafe(4), 0);
            }
            else
            {
                uint v_returned = 0;
                sbyte v = -1;
                for (int i = 0; i < 5 && v < 0; i++)
                {
                    byte vraw = Reader.ReadByte();
                    v = unchecked((sbyte)vraw);
                    v_returned = (uint)(v_returned * 0x80 + (vraw & 0x7f));
                }

                return v_returned;
            }
        }

        public int ReadInt32()
        {
            if ((OptimizationLevel & 0x20) == 0)
            {
                return BitConverter.ToInt32(ReadBytesEndiannessSafe(4), 0);
            }
            else
            {
                byte first_val = Reader.ReadByte();
                int v_returned = first_val & 0x3f;
                sbyte v = unchecked((sbyte)first_val);
                for (int i = 0; i < 4 && v < 0; i++)
                {
                    byte vraw = Reader.ReadByte();
                    v = unchecked((sbyte)vraw);
                    v_returned = v_returned * 0x80 + (vraw & 0x7f);
                }

                if ((first_val & 0x40) != 0)
                {
                    v_returned = -v_returned;
                }

                return v_returned;
            }
        }

        public string ReadString()
        {
            if (StringsBase != null)
            {
                uint offset = ReadUint32();
                int len;
                using (IEnumerator<byte> strstart = StringsBase.Skip((int)offset).GetEnumerator())
                {
                    strstart.MoveNext();
                    for (len = 0; strstart.Current != 0; len++)
                    {
                        strstart.MoveNext();
                    }
                }
                
                byte[] stringData = new byte[len];
                Array.ConstrainedCopy(StringsBase, (int)offset, stringData, 0, len);

                // string reading in ODBMemstream (ODBase.dll) is hardcoded to UTF-8
                return Encoding.UTF8.GetString(stringData);
            }
            else
            {
                long currentPos = Reader.BaseStream.Position;

                int len;
                for (len = 0; Reader.ReadByte() != 0; len++) { }
                
                Reader.BaseStream.Position = currentPos;

                byte[] stringData = Reader.ReadBytes(len);
                return Encoding.UTF8.GetString(stringData);
            }
        }

        public bool ReadBool()
        {
            return Reader.ReadBoolean();
        }

        public byte ReadByte()
        {
            return Reader.ReadByte();
        }

        public uint[] ReadMArray()
        {
            uint len;
            if ((OptimizationLevel & 0x20) == 0)
            {
                byte firstByte = Reader.ReadByte();

                if (firstByte == 0xff)
                {
                    len = ReadUint32();
                }
                else
                {
                    len = (uint)firstByte;
                }
            }
            else
            {
                len = ReadUint32();
            }

            uint[] result = new uint[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = ReadUint32();
            }

            return result;
        }

        private byte[] ReadBytesEndiannessSafe(int numBytes)
        {
            byte[] next = Reader.ReadBytes(numBytes);

            // odb files are little endian
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(next);
            }

            return next;
        }

        public virtual void Dispose()
        {
            Reader.Dispose();
        }
    }
}
