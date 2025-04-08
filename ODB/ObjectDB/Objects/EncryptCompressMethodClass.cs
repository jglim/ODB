using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class EncryptCompressMethodClass : ODBObject
    {
        private int Unknown1;
        private string String1;

        internal override void ParseFromReader(ODBReader reader)
        {
            if ((reader.OptimizationLevel >> 4 & 1) == 0)
            {
                reader.ReadByte();
            }

            Unknown1 = reader.ReadInt32();
            String1 = reader.ReadString();
        }

        public override string ToString()
        {
            return $"EncryptCompressMethodClass(Unknown1={Unknown1}, String1={String1}";
        }
    }
}
