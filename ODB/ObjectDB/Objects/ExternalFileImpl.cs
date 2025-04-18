using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDB.Objects
{
    public class ExternalFileImpl : ODBObject
    {
        public string FileName;
        public uint FileSize;
        public uint ObjectIndex;
        public int FileTypeEnum;


        internal override void ParseFromReader(ODBReader reader)
        {
            base.ParseFromReader(reader);

            if (ODBType >= 0x10600)
            {
                FileName = reader.ReadString(); // med40.jar "MCD3_GenericUdsFlashjob_MED40.jar" "immob.jar" "med40_abgleich_00_00_01.dll" "med40_flash_12_39_00.dll" "med40_sec_00_00_01.dll"
            }
            if (ODBType >= 0x10601)
            {
                FileSize = reader.ReadUint32(); // size 0x00025d7c 0x0002d85e 0x0002ca08 0x00008000 0x00018800 0x00008000
            }
            if (ODBType >= 0x10600)
            {
                ObjectIndex = reader.ReadUint32(); // 0x00002381 0x00002385 0x0000238e 0x00004075 0x00004078 0x0000407b
                // file upper bound is 4CB4 , 4075, 0014A244 , 4078, 0015226F  ,  407B, 0016AA9A
            }
            if (ODBType >= 0x10602)
            {
                FileTypeEnum = reader.ReadInt32(); // 0x00000001 1 1(javacode) 2(lib) 2  2  -- 0: <Undefined> JavaCode Library Flashdata
            }

        }

        public override string ToString()
        {
            return $"ExternalFileImpl(FileName={FileName})";
        }
    }
}
