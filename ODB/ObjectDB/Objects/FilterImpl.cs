using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class FilterImpl : ODBObject
    {
        public string String1;
        public byte[] Bytes1;
        public string String2;
        public byte[] Bytes2;
        public uint Uint1;
        public int FilterMode;


        internal override void ParseFromReader(ODBReader reader)
        {
            // somewhat like the odb base init but without the MArray

            if ((reader.OptimizationLevel >> 4 & 1) == 0)
            {
                reader.ReadByte();
            }

            int FilterMode = reader.ReadInt32();

            if (ODBType >= 0x10200)
            {
                Bytes1 = reader.ReadBytefield();
            }
            else
            {
                String1 = reader.ReadString();
            }

            if (FilterMode == 1)
            {
                if (ODBType >= 0x10200)
                {
                    Bytes2 = reader.ReadBytefield();
                }
                else
                {
                    String2 = reader.ReadString();
                }
            }
            else if (FilterMode == 2)
            {
                Uint1 = reader.ReadUint32();
            }

        }

        public override string ToString()
        {
            return $"TargetAddrOffsetImpl(FilterMode={FilterMode},String1={String1},Bytes1[{Bytes1.Length}])";
        }
    }
}
