using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class DescriptionClass : ODBObject
    {
        private string _String1;
        private string _String2;

        private ExternalDoc[] ExternalDocs;

        public DescriptionClass() { }

        internal override void ParseFromReader(ODBReader reader)
        {
            if ((reader.OptimizationLevel >> 4 & 1) == 0)
            {
                reader.ReadByte();
            }

            _String1 = reader.ReadString();

            // TODO: when odbtype < 0x10500, FUN_180111fd0 gets executed, which on first sight
            // seems to just return without parsing string2, but i haven't looked into this further
            if (ODBType > 0x10500)
            {
                _String2 = reader.ReadString();
            }

            uint numPImpls = reader.ReadUint32();

            if (numPImpls != 0)
            {
                for (int i = 0; i < numPImpls; i++)
                {
                    if ((reader.OptimizationLevel >> 4 & 1) == 0)
                    {
                        reader.ReadByte();
                    }
                }
            }

            if (ODBType < 0x10500)
            {
                return;
            }

            uint numExternalDocs = reader.ReadUint32();

            if (numExternalDocs != 0)
            {
                ExternalDocs = new ExternalDoc[numExternalDocs];
                for (int i = 0; i < numExternalDocs; i++)
                {
                    ExternalDoc doc = new ExternalDoc
                    {
                        ODBType = ODBType
                    };
                    doc.ParseFromReader(reader);

                    ExternalDocs[i] = doc;
                }
            }
        }

        public override string ToString()
        {
            string ret = $"DescriptionClass(String1={_String1}, String2={_String2}, ExternalDocs=[";
            
            for (int i = 0; ExternalDocs != null && i < ExternalDocs.Length; i++)
            {
                ret += ExternalDocs[i].ToString();
                if (i + 1 < ExternalDocs.Length)
                {
                    ret += ", ";
                }
            }
            ret += "])";

            return ret;
        }
    }
}
