using SctEditor.Util;

namespace SctEditor.Sct
{
    public class SctItem
    {
        public byte[] Data { get; set; }

        protected const int ItemPreambleSize = 16;
        
        public SctItem(byte[] rawData)
        {
            Data = rawData;
        }

        public static SctItem CreateFromStream(DataStream dsr, uint size)
        {
            // All we care about right now is if it's a dialog item or not.
            // Skip the preamble
            long startPosition = dsr.StreamPosition;
            
            byte[] rawData = dsr.ReadBytes((int)size);

            SctItem item;
            // Dialog items start with 5C 68 28
            if (size < ItemPreambleSize + 3 ||
                rawData[ItemPreambleSize + 0] != 0x5C ||
                rawData[ItemPreambleSize + 1] != 0x68 ||
                rawData[ItemPreambleSize + 2] != 0x28)
            {
                item = new SctItem(rawData);
            }
            else
            {
                item = new DialogItem(rawData);
            }
            dsr.StreamPosition = startPosition;
            return item;
        }
    }
}
