using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class NamedObject : ODBObject
    {
        private string String1;
        private string String2;

        private bool HasText;
        private bool HasDescription;

        public readonly TextClass Text;
        public readonly DescriptionClass Description;

        internal NamedObject()
        {
            Text = new TextClass();
            Description = new DescriptionClass();
        }

        internal override void ParseFromReader(ODBReader reader)
        {
            base.ParseFromReader(reader);

            String1 = reader.ReadString();
            if ((reader.OptimizationLevel >> 6 & 1) == 0)
            {
                String2 = reader.ReadString();
            }

            HasText = reader.ReadBool();
            if (HasText)
            {
                Text.ODBType = ODBType;
                Text.ParseFromReader(reader);
            }

            HasDescription = reader.ReadBool();
            if (HasDescription)
            {
                throw new Exception("'Description', a sub-object has not been implemented yet. this will lead to incorrect parsing");
                // (has been somewhat implemented, but not properly, so don't just uncomment the code)

                //Description.ODBType = ODBType;
                //Description.ParseFromReader(reader);
            }
        }

        public override string ToString()
        {
            return $"NamedObject(String1={String1}, String2={String2}, Text={Text}, Desc={Description}, ODBObject={base.ToString()})";
        }
    }
}
