using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class FlashClassImpl : NamedObject
    {
        private string String1;

        internal override void ParseFromReader(ODBReader reader)
        {
            base.ParseFromReader(reader);

            String1 = reader.ReadString();

        }

        public override string ToString()
        {
            return $"FlashClassImpl: {String1}";
        }
    }
}
