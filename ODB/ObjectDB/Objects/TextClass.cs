using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class TextClass : ODBObject
    {
        private string _String1;
        private string _String2;

        public string String1
        {
            get { return _String1; }
        }

        public string String2
        {
            get { return _String2; }
        }

        public TextClass() { }

        internal override void ParseFromReader(ODBReader reader)
        {
            if ((reader.OptimizationLevel >> 4 & 1) == 0)
            {
                reader.ReadByte();
            }

            _String1 = reader.ReadString();
            _String2 = reader.ReadString();
        }

        public override string ToString()
        {
            return $"TextClass(String1={_String1}, String2={_String2}";
        }
    }
}
