using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class DxDbBinaryFlashSegment
    {
        public byte[] Content;
        public uint Address;
        public uint Size;
        public override string ToString()
        {
            return $"Segment_0x{Address:X8}_0x{(Address + Size):X8}";
        }
    }
}
