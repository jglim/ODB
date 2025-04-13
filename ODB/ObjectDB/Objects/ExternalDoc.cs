using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    class ExternalDoc : ODBObject
    {
        private string String1;
        private string String2;

        public ExternalDoc()
        {
        }

        internal override void ParseFromReader(ODBReader reader)
        {
            if ((reader.OptimizationLevel >> 4 & 1) == 0)
            {
                reader.ReadByte();
            }

            if (0x104ff > ODBType)
            {
                String1 = reader.ReadString();
                String2 = reader.ReadString();
            }
        }

        public override string ToString()
        {
            return $"ExternalDoc(String1={String1}, String2={String2}";
        }
    }
}
