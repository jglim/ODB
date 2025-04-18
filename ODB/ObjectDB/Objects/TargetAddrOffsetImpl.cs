using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class TargetAddrOffsetImpl : ODBObject
    {
        public string String1;
        public byte[] Bytes1;
        public int AddressMode;


        internal override void ParseFromReader(ODBReader reader)
        {
            // somewhat like the odb base init but without the MArray
            if ((reader.OptimizationLevel >> 4 & 1) == 0)
            {
                reader.ReadByte();
            }

            int AddressMode = reader.ReadInt32();

            if (AddressMode == 1)
            {
                if (ODBType >= 0x10200)
                {
                    Bytes1 = reader.ReadBytefield();
                }
                else
                {
                    String1 = reader.ReadString();
                }
            }
            else if (AddressMode == 2)
            {
                if (ODBType >= 0x10200)
                {
                    Bytes1 = reader.ReadBytefield();
                }
                else
                {
                    String1 = reader.ReadString();
                }
            }

        }

        public override string ToString()
        {
            return $"TargetAddrOffsetImpl(AddressMode={AddressMode},String1={String1},Bytes1[{Bytes1.Length}])";
        }
    }
}
