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
        private const int SctItemStartOffset = 16;

        private List<SctItemHeader> _itemHeaders = new List<SctItemHeader>();

        public void AddItemHeader(SctItemHeader itemHeader)
        {
            _itemHeaders.Add(itemHeader);
        }

        public static SctFile CreateFromStream(DataStreamReader dsr)
        {
            SctFile file = new SctFile();

            // Read how many items there are.
            file.SctItemCount = dsr.ReadUint(SctItemCountOffset);
            // This then tells us the size of the item header section
            file.ItemHeaderSectionSize = file.SctItemCount * SctItemHeader.HeaderSize;

            // Read the items.
            for (uint i = 0; i < file.SctItemCount; i++)
            {
                uint offset = SctItemStartOffset + i * SctItemHeader.HeaderSize;
                SctItemHeader itemHeader = dsr.ReadSctItemHeader(offset);
                file.AddItemHeader(itemHeader);
                Console.WriteLine("Name: {0, -16}\tOffset: {1:X8}", itemHeader.Name, itemHeader.Offset);
            }

            return file;
        }
    }
}
