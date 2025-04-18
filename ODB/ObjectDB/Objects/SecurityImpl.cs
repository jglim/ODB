using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class SecurityImpl : ODBObject
    {
        internal override void ParseFromReader(ODBReader reader)
        {
            if ((reader.OptimizationLevel >> 4 & 1) == 0)
            {
                reader.ReadByte();
            }
            // fixme: break all 4 sub items into own odb objects


            // securitymethodimplparse 
            bool hasSecurityMethodImpl = reader.ReadBool();
            if (hasSecurityMethodImpl)
            {
                int sm = reader.ReadInt32();
                string s = reader.ReadString();
            }


            bool hasFwChecksumImpl = reader.ReadBool();
            if (hasFwChecksumImpl)
            {
                int sm = reader.ReadInt32();
                string s = reader.ReadString();
            }

            bool hasValidityForImpl = reader.ReadBool();
            if (hasValidityForImpl)
            {
                int sm = reader.ReadInt32();
                string s = reader.ReadString();
            }

            bool hasFwSignatureImpl = reader.ReadBool();
            if (hasFwSignatureImpl)
            {
                int sm = reader.ReadInt32();
                string s = reader.ReadString();
            }
        }

        public override string ToString()
        {
            return $"SecurityImpl()";
        }
    }
}

