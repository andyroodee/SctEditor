using SctEditor.Util;
using System.Collections.Generic;

namespace SctEditor.Sct
{
    public class SctItemHeader
    {
        public const uint HeaderSize = 20;
        public const int NameSize = 16;

        public string Name { get; set; }
        public uint Offset { get; set; }
        public uint DataOffset { get; set; }
        public uint DataSize { get; set; }
        public long PointerAdjust { get; set; }
        public List<long> FileNamePointers { get; set; }

        public static SctItemHeader CreateFromStream(DataStream dsr)
        {
            SctItemHeader itemHeader = new SctItemHeader();
            itemHeader.Offset = dsr.ReadUint();
            itemHeader.Name = dsr.ReadString(NameSize);
            itemHeader.FileNamePointers = new List<long>();
            return itemHeader;
        }

        public void WriteToStream(DataStream dsr)
        {
            dsr.WriteUint(Offset);
            dsr.WriteString(Name, NameSize);
        }
    }
}
