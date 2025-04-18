using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class DbRef
    {
        public byte Bitfield;
        public uint Unk1;
        public string String1;
        public string String2;
        public string String3;
        public byte Unk2;

        public override string ToString()
        {
            return $"DbRef: {Bitfield:X2}, Unk1: {Unk1}, Unk2: {Unk2}, String1:{String1}, String2:{String2}, String3:{String3} ";
        }
    }
}
