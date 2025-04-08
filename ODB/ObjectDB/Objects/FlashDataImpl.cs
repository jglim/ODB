using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class FlashDataImpl : NamedObject
    {
        private string String1;
        private string String2;

        private uint Unknown1;
        private bool HasUnknown1;

        private uint Unknown2;
        private bool HasUnknown2;

        private uint Unknown3;

        private DataFormatClass DataFormat;

        private DataFileClass DataFile;
        private bool HasDataFile;
        
        private EncryptCompressMethodClass EncryptCompressMethod;
        private bool HasEncryptCompressMethod;

        internal override void ParseFromReader(ODBReader reader)
        {
            base.ParseFromReader(reader);

            int firstReadInt = reader.ReadInt32();

            String1 = reader.ReadString();

            if (0x104ff < ODBType)
            {
                HasUnknown1 = reader.ReadBool();
                if (HasUnknown1)
                {
                    Unknown1 = reader.ReadUint32();
                }

                HasUnknown2 = reader.ReadBool();
                if (HasUnknown2)
                {
                    Unknown2 = reader.ReadUint32();
                }
            }

            if (0x10702 < ODBType)
            {
                String2 = reader.ReadString();
            }

            DataFormat = new DataFormatClass();
            DataFormat.ODBType = ODBType;
            DataFormat.ParseFromReader(reader);

            HasEncryptCompressMethod = reader.ReadBool();
            if (HasEncryptCompressMethod)
            {
                EncryptCompressMethod = new EncryptCompressMethodClass();
                EncryptCompressMethod.ODBType = ODBType;
                EncryptCompressMethod.ParseFromReader(reader);
            }

            // there's also a case for when ODBType is less than, but i haven't implemented that
            // since i haven't found a odb file with that version and the function that is called
            // in that case appears to just return without doing anything on first glance.
            if (firstReadInt == 1 && ODBType >= 0x10201)
            {
                Unknown3 = reader.ReadUint32();
            }
            else if (firstReadInt == 2)
            {
                HasDataFile = true;
                DataFile = new DataFileClass();
                DataFile.ODBType = ODBType;
                DataFile.ParseFromReader(reader);
            }
        }

        public override string ToString()
        {
            string str = $"FlashDataImpl(String1={String1}, String2={String2}, DataFormat={DataFormat}";

            str += $", HasEncryptCompressMethod={HasEncryptCompressMethod}";

            if (HasEncryptCompressMethod)
            {
                str += $", EncryptCompressMethod={EncryptCompressMethod}";
            }

            str += $", HasDataFile={HasDataFile}";
            if (HasDataFile)
            {
                str += $", DataFile={DataFile}";
            }

            str += $", NamedObject={base.ToString()}";

            str += ")";

            return str;
        }
    }
}
