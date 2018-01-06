using Extensions;
using System.IO;

namespace SctEditor.Aklz
{
    public class AKLZ
    {
        public static MemoryStream Decompress(Stream data)
        {
            try
            {
                data.Position = 0;
                byte[] aklzBuffer = new byte[4];
                data.Read(aklzBuffer, 0, 4);
                if (aklzBuffer[0] != 0x41 ||
                    aklzBuffer[1] != 0x4B ||
                    aklzBuffer[2] != 0x4C ||
                    aklzBuffer[3] != 0x5A)
                {
                    return new MemoryStream(StreamReaderExtensions.ToByteArray(data));
                }
                const uint START_INDEX = 0x1000;
                // Compressed & Decompressed Data Information
                uint compressedSize = (uint)data.Length;
                uint decompressedSize = EndianUtil.SwapEndian(StreamReaderExtensions.ReadUInt(data, 0xC));

                uint sourcePointer = 0x10;
                uint destPointer = 0x0;

                byte[] compressedData = StreamReaderExtensions.ToByteArray(data);
                byte[] decompressedData = new byte[decompressedSize];

                // Start Decompression
                while (sourcePointer < compressedSize && destPointer < decompressedSize)
                {
                    byte instruction = compressedData[sourcePointer]; // Compression Flag
                    sourcePointer++;

                    for (int i = 0; i < 8; ++i)
                    {
                        bool copySingleByte = (instruction & 0x01) != 0;
                        instruction >>= 1;
                        if (copySingleByte) // Data is not compressed
                        {
                            decompressedData[destPointer] = compressedData[sourcePointer];
                            sourcePointer++;
                            destPointer++;
                        }
                        else // Data is compressed
                        {
                            int copyFromAddress = (compressedData[sourcePointer] | ((compressedData[sourcePointer + 1] & 0xF0) << 4)) + 0x12;
                            int Amount = (compressedData[sourcePointer + 1] & 0x0F) + 3;
                            sourcePointer += 2;

                            int memCopyAddress = copyFromAddress;
                            uint wrapCount = destPointer / START_INDEX;
                            for (int wrap = 1; wrap <= wrapCount; ++wrap)
                            {
                                if (copyFromAddress + wrap * START_INDEX < destPointer)
                                {
                                    memCopyAddress += (int)START_INDEX;
                                }
                            }

                            if (memCopyAddress > destPointer)
                            {
                                memCopyAddress -= (int)START_INDEX;
                            }

                            // Copy copySize bytes from decompressedData
                            for (int copyIndex = 0; copyIndex < Amount; ++copyIndex, ++memCopyAddress)
                            {
                                if (memCopyAddress < 0)
                                {
                                    // This means 0
                                    decompressedData[destPointer] = 0;
                                }
                                else
                                {
                                    decompressedData[destPointer] = decompressedData[memCopyAddress];
                                }
                                ++destPointer;
                                if (destPointer >= decompressedData.Length)
                                {
                                    return new MemoryStream(decompressedData);
                                }
                            }
                        }

                        // Check for out of range
                        if (sourcePointer >= compressedSize || destPointer >= decompressedSize)
                        {
                            break;
                        }
                    }
                }

                return new MemoryStream(decompressedData);
            }
            catch
            {
                return null; // An error occured while decompressing
            }
        }
        
        public static MemoryStream Compress(Stream data)
        {
            try
            {
                uint DecompressedSize = (uint)data.Length;

                MemoryStream CompressedData = new MemoryStream();
                byte[] DecompressedData = StreamReaderExtensions.ToByteArray(data);

                uint SourcePointer = 0x0;
                uint DestPointer = 0x10;

                // Set up the Lz Compression Dictionary
                LzWindowDictionary LzDictionary = new LzWindowDictionary();
                LzDictionary.SetWindowSize(0x1000);
                LzDictionary.SetMaxMatchAmount(0xF + 3);

                // Start compression
                StreamWriterExtensions.Write(CompressedData, "AKLZ");
                byte[] header = new byte[] { 0x7e, 0x3f, 0x51, 0x64, 0x3d, 0xcc, 0xcc, 0xcd };
                StreamWriterExtensions.Write(CompressedData, header);
                StreamWriterExtensions.Write(CompressedData, EndianUtil.SwapEndian(DecompressedSize));
                while (SourcePointer < DecompressedSize)
                {
                    byte Flag = 0x0;
                    uint FlagPosition = DestPointer;
                    CompressedData.WriteByte(Flag); // It will be filled in later
                    DestPointer++;

                    for (int i = 0; i < 8; ++i)
                    {
                        int[] LzSearchMatch = LzDictionary.Search(DecompressedData, SourcePointer, DecompressedSize);
                        if (LzSearchMatch[1] > 0) // There is a compression match
                        {
                            Flag |= (byte)(0 << i);

                            int copySize = LzSearchMatch[1] - 3;
                            int address = LzSearchMatch[0] - 0x12;
                            byte firstByte = (byte)(address & 0x0FF);
                            byte secondByte = (byte)(copySize | ((address & 0xF00) >> 4));
                            CompressedData.WriteByte(firstByte);
                            CompressedData.WriteByte(secondByte);

                            LzDictionary.AddEntryRange(DecompressedData, (int)SourcePointer, LzSearchMatch[1]);
                            LzDictionary.SlideWindow(LzSearchMatch[1]);

                            SourcePointer += (uint)LzSearchMatch[1];
                            DestPointer += 2;
                        }
                        else // There wasn't a match
                        {
                            Flag |= (byte)(1 << i);

                            CompressedData.WriteByte(DecompressedData[SourcePointer]);

                            LzDictionary.AddEntry(DecompressedData, (int)SourcePointer);
                            LzDictionary.SlideWindow(1);

                            SourcePointer++;
                            DestPointer++;
                        }

                        // Check for out of bounds
                        if (SourcePointer >= DecompressedSize)
                        {
                            break;
                        }
                    }

                    // Write the flag.
                    // Note that the original position gets reset after writing.
                    CompressedData.Seek(FlagPosition, SeekOrigin.Begin);
                    CompressedData.WriteByte(Flag);
                    CompressedData.Seek(DestPointer, SeekOrigin.Begin);
                }

                return CompressedData;
            }
            catch
            {
                return null; // An error occured while compressing
            }
        }
    }
}