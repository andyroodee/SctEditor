using SctEditor.Util;
using System;
using System.Collections.Generic;

namespace SctEditor.Sct
{
    public enum Endianness
    {
        BigEndian,
        LittleEndian
    }

    public class SctFile
    {
        public uint SctItemCount { get; set; }
        public uint ItemHeaderSectionSize { get; set; }
                
        private const int SctItemCountOffset = 8;
        private const int SctItemStartOffset = 12;

        // The first 8 bytes in the file should not be changed, so we'll record them here.
        public byte[] FileHeaderPreamble { get; private set; }

        public List<SctItemHeader> ItemHeaders { get; private set; }
        public List<SctItem> Items { get; private set; }

        public SctFile()
        {
            FileHeaderPreamble = new byte[SctItemCountOffset];
            ItemHeaders = new List<SctItemHeader>();
            Items = new List<SctItem>();
    }

        public void AddItemHeader(SctItemHeader itemHeader)
        {
            ItemHeaders.Add(itemHeader);
        }

        public static SctFile CreateFromStream(DataStreamReader dsr)
        {
            SctFile file = new SctFile();

            file.FileHeaderPreamble = dsr.ReadBytes(SctItemCountOffset);
            // Read how many items there are.
            file.SctItemCount = dsr.ReadUint(SctItemCountOffset);
            // This then tells us the size of the item header section
            file.ItemHeaderSectionSize = file.SctItemCount * SctItemHeader.HeaderSize;

            // Read the item headers.
            for (uint i = 0; i < file.SctItemCount; i++)
            {
                uint offset = SctItemStartOffset + i * SctItemHeader.HeaderSize;
                SctItemHeader itemHeader = dsr.ReadSctItemHeader(offset);
                if (i > 0)
                {
                    // Note: this assumes sequential ordering. If that proves false, we'd need to sort by offset first.
                    uint prevItemSize = itemHeader.Offset - file.ItemHeaders[(int)i - 1].Offset;
                    file.ItemHeaders[(int)i - 1].DataSize = prevItemSize;
                    Console.WriteLine("Data size: " + prevItemSize);
                }
                Console.WriteLine("Name: {0, -16}\tOffset: {1:X8}", itemHeader.Name, itemHeader.Offset);

                itemHeader.DataOffset = SctItemStartOffset + file.ItemHeaderSectionSize + itemHeader.Offset;
                Console.WriteLine("Data at: {0:X8}", itemHeader.DataOffset);

                file.AddItemHeader(itemHeader);
            }

            // Size of the final item is "whatever was left"
            uint finalItemSize = (uint)dsr.Length - file.ItemHeaders[(int)file.SctItemCount - 1].DataOffset;
            file.ItemHeaders[(int)file.SctItemCount - 1].DataSize = finalItemSize;

            // Now that we know the header information, we can read in the data blocks.
            for (int i = 0; i < file.ItemHeaders.Count; i++)
            {
                dsr.StreamPosition = file.ItemHeaders[i].DataOffset;
                var item = SctItem.CreateFromStream(dsr, file.ItemHeaders[i].DataSize);

                file.Items.Add(item);
            }

            return file;
        }
    }
}
