using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class DataFileClass : ODBObject
    {
        private bool Unknown1;
        private string Filename;

        internal override void ParseFromReader(ODBReader reader)
        {
            if ((reader.OptimizationLevel >> 4 & 1) == 0)
            {
                reader.ReadByte();
            }

            Unknown1 = reader.ReadBool();
            Filename = reader.ReadString();
        }

        public override string ToString()
        {
            return $"DataFileClass(Unknown1={Unknown1}, Filename={Filename}";
        }
    }
}
