using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class TeamMemberImpl : NamedObject
    {
        private string String1;
        private string String2;
        private string String3;
        private string String4;
        private string String5;
        private string String6;
        private string String7;
        private string String8;

        internal override void ParseFromReader(ODBReader reader)
        {
            // fixme: incomplete
            base.ParseFromReader(reader);
            String1 = reader.ReadString();
            String2 = reader.ReadString();
            String3 = reader.ReadString();
            String4 = reader.ReadString();
            String5 = reader.ReadString();
            String6 = reader.ReadString();
            String7 = reader.ReadString();
            String8 = reader.ReadString();

            if (ODBType >= 0x10201)
            {
                String1 = reader.ReadString();
            }

        }

        public override string ToString()
        {
            return $"TeamMemberImpl({String1}, {String2}, {String3}, {String4}, {String5}, {String6}, {String7}, {String8})";
        }
    }
}
