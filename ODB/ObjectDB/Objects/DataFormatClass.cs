using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class DataFormatClass : ODBObject
    {
        private int DataFormatSelection;
        private string DataFormatName;

        private string String1;

        internal override void ParseFromReader(ODBReader reader)
        {
            if ((reader.OptimizationLevel >> 4 & 1) == 0)
            {
                reader.ReadByte();
            }
            
            DataFormatSelection = reader.ReadInt32();

            if (ODBType < 0x10500)
            {
                String1 = reader.ReadString();
            }

            if (0x104ff < ODBType)
            {
                DataFormatName = reader.ReadString();
            }

            if (ODBType < 0x10705 && 0x104ff < ODBType)
            {
                FixDataformatSelection();
            }
        }

        private void FixDataformatSelection()
        {
            Dictionary<int, string> formatNames = new Dictionary<int, string>()
            {
                {0, "<Undefined>"},
                {1, "IntelHex"},
                {2, "MotorolaS"},
                {3, "Binary"},
                {8, "UserDefined"},
            };

            Dictionary<int, string> formatNames2 = new Dictionary<int, string>()
            {
                {0,  "<Undefined>"},
                {4,  "MotorolaSZipped"},
                {5,  "IntelHexZipped"},
                {6,  "SegmentedBinaryFromMot"},
                {7,  "SegmentedBinaryFromIntel"},
            };

            if (!formatNames.ContainsKey(DataFormatSelection))
            {
                if (!formatNames2.ContainsKey(DataFormatSelection))
                {
                    DataFormatSelection = 0;
                }
                else
                {
                    DataFormatName = formatNames2[DataFormatSelection];
                    DataFormatSelection = 8;
                }
            }
        }

        public override string ToString()
        {
            return $"DataFormatClass(DataFormatSelection={DataFormatSelection}, DataFormatName={DataFormatName}, String1={String1})";
        }
    }
}
