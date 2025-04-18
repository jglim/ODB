using System;
using System.Collections.Generic;
using System.Text;
using System.IO;    

namespace ObjectDB.Objects
{
    public class DxDbBinaryFlashContent
    {
        public uint Version;
        public DxDbBinaryFlashSegment[] Segments;
        public DxDbBinaryFlashContent(byte[] contentBytes) 
        {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(contentBytes)))
            {
                List<DxDbBinaryFlashSegment> segments = new List<DxDbBinaryFlashSegment>();
                Version = reader.ReadUInt32();
                uint segmentCount = reader.ReadUInt32();
                for (uint i = 0; i < segmentCount; i++) 
                {
                    uint address = reader.ReadUInt32();
                    uint size = reader.ReadUInt32();
                    segments.Add(new DxDbBinaryFlashSegment { Address = address, Size = size });
                }
                for (int i = 0; i < segmentCount; i++) 
                {
                    segments[i].Content = reader.ReadBytes((int)segments[i].Size);
                }
                Segments = segments.ToArray();
            }
        }
    }
}
