using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualBasic;

namespace ObjectDB.Objects
{
    public class Category : NamedObject
    {
        private string String1;
        private int Unknown1;
        private uint Unknown2;
        private uint[] MArray2;
        private uint[] MArray3;

        internal override void ParseFromReader(ODBReader reader)
        {
            base.ParseFromReader(reader);

            String1 = reader.ReadString();

            if (0x10201 < ODBType)
            {
                Unknown1 = reader.ReadInt32();
            }

            Unknown2 = reader.ReadUint32();
            MArray2 = reader.ReadMArray();

            if (0x104ff < ODBType)
            {
                MArray3 = reader.ReadMArray();
            }
        }

        public override string ToString()
        {
            return $"Category(String1={String1}, Unknown2={Unknown2}, MArray2={MArray2}, NamedObject={base.ToString()})";
        }
    }
}
