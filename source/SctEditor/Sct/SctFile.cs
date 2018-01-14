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

        private List<SctItemHeader> ItemHeaders { get; set; }
        public List<SctItem> Items { get; private set; }

        private long _filenamePointerAdjust = 0;

        private class PointerRecord
        {
            public int TargetIndex { get; set; }
            public long Offset { get; set; }
        }

        private Dictionary<int, List<PointerRecord>> PointerRecords { get; set; }

        public SctFile()
        {
            FileHeaderPreamble = new byte[SctItemCountOffset];
            ItemHeaders = new List<SctItemHeader>();
            Items = new List<SctItem>();
            PointerRecords = new Dictionary<int, List<PointerRecord>>();
        }

        public void AddItemHeader(SctItemHeader itemHeader)
        {
            ItemHeaders.Add(itemHeader);
        }

        private void ReadItemHeaders(DataStream ds)
        {
            for (uint i = 0; i < SctItemCount; i++)
            {
                uint offset = SctItemStartOffset + i * SctItemHeader.HeaderSize;
                SctItemHeader itemHeader = ds.ReadSctItemHeader(offset);
                if (i > 0)
                {
                    // Note: this assumes sequential ordering. If that proves false, we'd need to sort by offset first.
                    uint prevItemSize = itemHeader.Offset - ItemHeaders[(int)i - 1].Offset;
                    ItemHeaders[(int)i - 1].DataSize = prevItemSize;
                }

                itemHeader.DataOffset = SctItemStartOffset + ItemHeaderSectionSize + itemHeader.Offset;
                AddItemHeader(itemHeader);
            }
        }

        private void ReadDataBlocks(DataStream ds)
        {
            for (int i = 0; i < ItemHeaders.Count; i++)
            {
                ds.StreamPosition = ItemHeaders[i].DataOffset;
                var item = SctItem.CreateFromStream(ds, ItemHeaders[i].DataSize);

                bool isLastItem = (i == ItemHeaders.Count - 1);
                uint start = ItemHeaders[i].DataOffset;
                DataStream dataStream = new DataStream(new MemoryStream(item.Data), Endianness.BigEndian);
                for (long offset = 0; offset < ItemHeaders[i].DataSize; offset += 4)
                {
                    long relOffset = dataStream.ReadUint();
                    long absOffset = relOffset + start + offset;
                    long filenameSectionOffset = ItemHeaders[ItemHeaders.Count - 1].DataOffset;
                    // Check if the offset points to one of the filenames at the end of the file.
                    if (!isLastItem && absOffset >= filenameSectionOffset && absOffset < ds.Length)
                    {
                        long oldPos = ds.StreamPosition;
                        ds.Stream.Seek(absOffset, SeekOrigin.Begin);
                        int nameLength = 0;
                        while (true)
                        {
                            byte b = ds.ReadByte();
                            char c = (char)b;
                            if (char.IsLetterOrDigit(c) || c == '.' || c == '_')
                            {
                                nameLength++;
                            }
                            else if (b == 0 && nameLength > 0)
                            {
                                ItemHeaders[i].FileNamePointers.Add(offset);
                                break;
                            }
                            else
                            {
                                break;
                            }

                        }
                        ds.Stream.Seek(oldPos, SeekOrigin.Begin);
                    }
                    if (absOffset <= int.MaxValue)
                    {
                        for (int j = 0; j < ItemHeaders.Count; j++)
                        {
                            if (i != j && ItemHeaders[j].DataOffset == absOffset)
                            {
                                if (!PointerRecords.ContainsKey(i))
                                {
                                    PointerRecords.Add(i, new List<PointerRecord>());
                                }
                                var pointersForItem = PointerRecords[i];
                                pointersForItem.Add(new PointerRecord { TargetIndex = j, Offset = offset });
                            }
                        }
                    }
                }

                Items.Add(item);
            }
        }

        public static SctFile CreateFromStream(DataStream ds)
        {
            SctFile file = new SctFile();

            file.FileHeaderPreamble = ds.ReadBytes(SctItemCountOffset);
            // Read how many items there are.
            file.SctItemCount = ds.ReadUint(SctItemCountOffset);
            // This then tells us the size of the item header section
            file.ItemHeaderSectionSize = file.SctItemCount * SctItemHeader.HeaderSize;

            // Read the item headers
            file.ReadItemHeaders(ds);

            // FIXME: Size of the final item is "whatever was left", but that's not really correct.
            // The size of the common ending blocks (with all the LIG_* and VIB_* items) is fixed in all files, so
            // we could work it out that way.
            uint finalItemSize = (uint)ds.Length - file.ItemHeaders[(int)file.SctItemCount - 1].DataOffset;
            file.ItemHeaders[(int)file.SctItemCount - 1].DataSize = finalItemSize;

            // Now that we know the header information, we can read in the data blocks.
            file.ReadDataBlocks(ds);

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
                // When items are resized, we need to adjust any internal pointers to match new locations.
                ItemHeaders[i].PointerAdjust = offset - ItemHeaders[i].Offset;
                // We need to track the cumulative filename pointer adjust, since they're at the end of the file
                // past all the other items.
                if (ItemHeaders[i].PointerAdjust > _filenamePointerAdjust)
                {
                    _filenamePointerAdjust = ItemHeaders[i].PointerAdjust;
                }
                ItemHeaders[i].Offset = offset;
                ItemHeaders[i].WriteToStream(dsr);
                offset += (uint)dataBlocks[i].Length;
            }
            for (int i = 0; i < dataBlocks.Count; i++)
            {
                AdjustPointers(i);
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

        private void AdjustPointers(int itemIndex)
        {
            DataStream ds = new DataStream(new MemoryStream(Items[itemIndex].Data), Endianness.BigEndian);

            // Adjust the file name pointers
            for (int i = 0; i < ItemHeaders[itemIndex].FileNamePointers.Count; i++)
            {
                var offset = ItemHeaders[itemIndex].FileNamePointers[i];
                var newPointer = ds.ReadUint(offset) + _filenamePointerAdjust;
                UpdatePointerValue(itemIndex, newPointer, offset);
            }

            if (PointerRecords.ContainsKey(itemIndex))
            {                
                var records = PointerRecords[itemIndex];
                for (int i = 0; i < records.Count; i++)
                {
                    var targetItem = ItemHeaders[records[i].TargetIndex];
                    if (targetItem.PointerAdjust > 0)
                    {
                        var offset = records[i].Offset;
                        var newPointer = ds.ReadUint(offset) + targetItem.PointerAdjust;
                        UpdatePointerValue(itemIndex, newPointer, offset);
                    }
                }
            }

            ds.Stream.Close();
        }

        private void UpdatePointerValue(int itemIndex, long newPointer, long offset)
        {
            Items[itemIndex].Data[offset + 0] = (byte)((newPointer & 0xff000000) >> 24);
            Items[itemIndex].Data[offset + 1] = (byte)((newPointer & 0x00ff0000) >> 16);
            Items[itemIndex].Data[offset + 2] = (byte)((newPointer & 0x0000ff00) >> 8);
            Items[itemIndex].Data[offset + 3] = (byte)(newPointer & 0x000000ff);
        }
    }
}
