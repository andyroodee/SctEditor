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

        private long _filenamePointerAdjust = 0;

        public class PointerRecord
        {
            public int TargetIndex { get; set; }
            public long Offset { get; set; }
        }

        public Dictionary<int, PointerRecord> PointerRecords { get; set; }

        public SctFile()
        {
            FileHeaderPreamble = new byte[SctItemCountOffset];
            ItemHeaders = new List<SctItemHeader>();
            Items = new List<SctItem>();
            PointerRecords = new Dictionary<int, PointerRecord>();
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
                //if (pugs.Contains(itemHeader.Offset))
                //{
                //    System.Console.WriteLine("{0:X4} {3, -16} Offset: {1:X8} Data Offset: {2:X8}", i, itemHeader.Offset, itemHeader.DataOffset, itemHeader.Name);
                //}
                //if (pugs.Contains(itemHeader.DataOffset))
                //{
                   // System.Console.WriteLine("Data Offset: {0:X8}", itemHeader.DataOffset);
                //}

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

                uint start = file.ItemHeaders[i].DataOffset;
                DataStream ditty = new DataStream(new MemoryStream(item.Data), Endianness.BigEndian);
                //using (MemoryStream lol = new MemoryStream(item.Data))
                {
                    //long offset = 0;
                    for (long offset = 0; offset < file.ItemHeaders[i].DataSize; offset += 4)
                    {
                        // var val = ditty.ReadUint() + offset;
                        long boo = ditty.ReadUint();
                        // var val = ditty.ReadUint() + start + offset;
                        long val = boo + start + offset;
                        long filenameSectionOffset = file.ItemHeaders[file.ItemHeaders.Count - 1].DataOffset;
                        if (val >= filenameSectionOffset && val < dsr.Length)
                        {
                            long oldPos = dsr.StreamPosition;
                            dsr.Stream.Seek(val, SeekOrigin.Begin);
                            int nameLength = 0;
                            while (true)
                            {
                                byte b = dsr.ReadByte();
                                if (char.IsLetterOrDigit((char)b) || (char)b == '.')
                                {
                                    nameLength++;
                                }
                                else if (b == 0 && nameLength > 0)
                                {
                                    file.ItemHeaders[i].FileNamePointers.Add(offset + start);
                                    System.Console.WriteLine("{1} Could point to file name at {0:X8}", offset + start, file.ItemHeaders[i].Name);
                                    break;
                                }
                                else
                                {
                                    break;
                                }

                            }                           
                            dsr.Stream.Seek(oldPos, SeekOrigin.Begin);
                        }
                        if (val <= int.MaxValue)
                        {
                            for (int j = 0; j < file.ItemHeaders.Count; j++)
                            {
                                if (i != j && file.ItemHeaders[j].DataOffset == val)
                                {
                                    file.PointerRecords.Add(i, new PointerRecord { TargetIndex = j, Offset = offset });
                                    System.Console.WriteLine("Possible pointer from {0} to {1} at {2:X8}", file.ItemHeaders[i].Name, file.ItemHeaders[j].Name, offset + start);
                                }
                            }
                        }
                    }
                }
                
                //System.Console.WriteLine("Data size: " + file.ItemHeaders[i].DataSize);

               // File.WriteAllBytes(file.ItemHeaders[i].Name, item.Data);

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
            // Adjust the file name pointers
            for (int i = 0; i < ItemHeaders[itemIndex].FileNamePointers.Count; i++)
            {
                ItemHeaders[itemIndex].FileNamePointers[i] += _filenamePointerAdjust;
            }

            uint start = ItemHeaders[itemIndex].DataOffset;
            DataStream ditty = new DataStream(new MemoryStream(Items[itemIndex].Data), Endianness.BigEndian);
            for (long offset = 0; offset < ditty.Length; offset += 4)
            {
                // var val = ditty.ReadUint() + offset;
                long boo = ditty.ReadUint();
                // var val = ditty.ReadUint() + start + offset;
                long val = boo + start + offset;
                
                if (val <= int.MaxValue)
                {
                    for (int i = 0; i < ItemHeaders.Count; i++)
                    {
                        if (itemIndex != i && ItemHeaders[i].DataOffset == val)
                        {
                            System.Console.WriteLine("Possible pointer from {0} to {1} at {2:X8}", ItemHeaders[i].Name, ItemHeaders[i].Name, offset + start);
                        }
                    }
                }
            }
        }
    }
}
