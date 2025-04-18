using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class DataBlockImpl : NamedObject
    {
        private string String1;
        private string String2;

        private bool HasUnknown1;
        private byte[] Unknown1;

        private uint[] MArray1;
        private uint FilterCount;

        private bool HasTargetAddrOffset;
        private byte[] Unknown2;

        private bool HasAudience;


        internal override void ParseFromReader(ODBReader reader)
        {
            base.ParseFromReader(reader);

            String1 = reader.ReadString();
            String2 = reader.ReadString();

            if (0x104ff < ODBType)
            {
                HasUnknown1 = reader.ReadBool();
                if (HasUnknown1)
                {
                    throw new Exception("has bytefield");
                }
            }

            MArray1 = reader.ReadMArray();

            FilterCount = reader.ReadUint32();
            for (int i = 0; i < FilterCount; i++)
            {
                var filterImpl = new FilterImpl();
                filterImpl.ODBType = ODBType;
                filterImpl.ParseFromReader(reader);
            }

            // checks if (0x104ff < ODBType) but both paths read a dbref
            DbRef dbRef = reader.ReadDbRef();

            HasTargetAddrOffset = reader.ReadBool();
            if (HasTargetAddrOffset)
            {
                var targetAddrOffsetImpl = new TargetAddrOffsetImpl();
                targetAddrOffsetImpl.ODBType = ODBType;
                targetAddrOffsetImpl.ParseFromReader(reader);
            }

            var arr1 = reader.ReadMArray(); // 3 obj, d/e/f ptrs to obj list?
            var arr2 = reader.ReadMArray();

            var securitiesCount = reader.ReadUint32();
            for (int i = 0; i < securitiesCount; i++)
            {
                var securityImpl = new SecurityImpl();
                securityImpl.ODBType = ODBType;
                securityImpl.ParseFromReader(reader);
            }


            if (ODBType >= 0x10500)
            {
                HasAudience = reader.ReadBool();
                if (HasAudience)
                {
                    throw new Exception("vdxaudience parse unimplemented");
                }
            }

            Console.WriteLine();

        }

        public override string ToString()
        {
            return $"DataBlockImpl: {String1}, {String2}";
        }
    }
}
