using Extensions;
using SctEditor.Util;
using System.Collections.Generic;
using System.IO;

namespace SctEditor.Sct
{
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

        public static SctFile CreateFromStream(DataStream dsr)
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
                }

                itemHeader.DataOffset = SctItemStartOffset + file.ItemHeaderSectionSize + itemHeader.Offset;

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

        public void SaveToFile(string filename, Endianness endianness)
        {
            DataStream dsr = new DataStream(new MemoryStream(), endianness);
            dsr.WriteBytes(FileHeaderPreamble);
            dsr.WriteUint(SctItemCount);

            // First figure out the final sizes of the data blocks, so that we can write proper values to headers.
            List<byte[]> dataBlocks = new List<byte[]>((int)SctItemCount);
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] is DialogItem)
                {
                    var dialogItem = (DialogItem)Items[i];
                    dataBlocks.Add(dialogItem.ToByteArray());
                }
                else
                {
                    dataBlocks.Add(Items[i].Data);
                }
            }

            uint offset = 0;
            for (int i = 0; i < ItemHeaders.Count; i++)
            {
                ItemHeaders[i].Offset = offset;
                ItemHeaders[i].WriteToStream(dsr);
                offset += (uint)dataBlocks[i].Length;
            }
            for (int i = 0; i < dataBlocks.Count; i++)
            {
                dsr.WriteBytes(dataBlocks[i]);
            }
            if (endianness == Endianness.BigEndian)
            {
                var compressed = Aklz.AKLZ.Compress(dsr.Stream);
                File.WriteAllBytes(filename, compressed.ToArray());
            }
            else
            {
                File.WriteAllBytes(filename, dsr.Stream.ToByteArray());
            }
            dsr.Stream.Close();
        }
    }
}
