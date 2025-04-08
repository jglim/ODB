using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB
{
    public class ODBObject
    {
        private int _ODBType;
        private bool ODBTypeSet = false;

        // making it like this because i don't want to repeat ODB type throughout every constructor
        internal int ODBType
        {
            get { return _ODBType; }
            set
            {
                if (!ODBTypeSet)
                {
                    _ODBType = value;
                    ODBTypeSet = true;
                }
                else
                {
                    throw new Exception("ODB Type can only be set once to prevent parsing confusion");
                }
            }
        }

        private uint Unknown1;

        public ODBObject() { }

        internal virtual void ParseFromReader(ODBReader reader)
        {
            if ((reader.OptimizationLevel >> 4 & 1) == 0)
            {
                reader.ReadByte(); // not used it seems
            }

            if ((reader.OptimizationLevel >> 2 & 1) == 0)
            {
                reader.ReadMArray(); // also not used
            }

            Unknown1 = reader.ReadUint32();
        }

        public override string ToString()
        {
            return $"ODBObject (Unknown1={Unknown1})";
        }
    }
}
