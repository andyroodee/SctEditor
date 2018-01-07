using SctEditor.Util;

namespace SctEditor.Sct
{
    public class SctItem
    {
        public byte[] Data { get; set; }

        private const int ItemPreambleSize = 16;

        public static SctItem CreateFromStream(DataStream dsr, uint size)
        {
            // All we care about right now is if it's a dialog item or not.
            // Skip the preamble
            long startPosition = dsr.StreamPosition;
            dsr.StreamPosition += ItemPreambleSize;
            SctItem item;
            // Dialog items start with 5C 68 28
            byte[] startBytes = dsr.ReadBytes(3);
            if (startBytes[0] != 0x5C || 
                startBytes[1] != 0x68 || 
                startBytes[2] != 0x28)
            {
                item = new SctItem();
            }
            else
            {
                item = new DialogItem();
            }
            dsr.StreamPosition = startPosition;
            item.Data = dsr.ReadBytes((int)size);
            return item;
        }
    }
}
