using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualBasic;

namespace ObjectDB.Objects
{
    public class VdxFlashImpl : Category
    {
        private string String1;
        private uint[] MArray1;
        private uint[] MArray2;
        private uint[] MArray4;

        public VdxFlashImpl() { }

        internal override void ParseFromReader(ODBReader reader)
        {
            base.ParseFromReader(reader);

            String1 = reader.ReadString();
            MArray1 = reader.ReadMArray();
            MArray2 = reader.ReadMArray();

            if (0x104ff < ODBType)
            {
                MArray4 = reader.ReadMArray();
            }
        }

        public override string ToString()
        {
            return $"VDXFlashImpl(String1={String1}, MArray1={MArray1.Length}, MArray2={MArray2.Length}, MArray4={MArray4.Length}, base={base.ToString()}";
        }
    }
}
