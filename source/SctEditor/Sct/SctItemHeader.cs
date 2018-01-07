using SctEditor.Util;

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

        public static SctItemHeader CreateFromStream(DataStreamReader dsr)
        {
            SctItemHeader itemHeader = new SctItemHeader();
            itemHeader.Offset = dsr.ReadUint();
            itemHeader.Name = dsr.ReadString(NameSize);
            return itemHeader;
        }
    }
}
