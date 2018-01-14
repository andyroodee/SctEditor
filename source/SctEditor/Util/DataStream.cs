// An Endian-aware wrapper around a Stream.
// It assumes that the host computer reading the stream is little endian.

using SctEditor.Sct;
using System;
using System.IO;
using System.Text;

namespace SctEditor.Util
{
    public class DataStream
    {
        public Stream Stream { get; private set; }

        public Endianness Endianness { get; private set; }

        public long StreamPosition
        {
            get { return Stream.Position; }
            set { Stream.Position = value; }
        }

        public long Length
        {
            get { return Stream.Length; }
        }
        
        public DataStream(Stream stream, Endianness endianness)
        {
            Stream = stream;
            Endianness = endianness;
        }
        
        public SctItemHeader ReadSctItemHeader(long offset)
        {
            StreamPosition = offset;
            SctItemHeader itemHeader = SctItemHeader.CreateFromStream(this);
            return itemHeader;
        }
        
        public byte[] ReadBytes(int length)
        {
            byte[] array = new byte[length];
            Stream.Read(array, 0, length);
            return array;
        }

        public byte ReadByte()
        {
            return (byte)Stream.ReadByte();
        }

        public string ReadString(long offset, int maxLength)
        {
            StreamPosition = offset;

            StringBuilder sb = new StringBuilder(maxLength);

            for (int i = 0; i < maxLength; i++)
            {
                char c = (char)Stream.ReadByte();
                if (c != '\0')
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public string ReadString(int maxLength)
        {
            return ReadString(StreamPosition, maxLength);
        }

        public uint ReadUint(long offset)
        {
            byte[] array = new byte[4];
            StreamPosition = offset;
            Stream.Read(array, 0, 4);
            var value = BitConverter.ToUInt32(array, 0);
            if (Endianness == Endianness.BigEndian)
            {
                return EndianUtil.SwapEndian(value);
            }
            else
            {
                return value;
            }
        }

        public uint ReadUint()
        {
            return ReadUint(StreamPosition);
        }

        public void WriteBytes(byte[] values)
        {
            Stream.Write(values, 0, values.Length);
        }

        public void WriteUint(uint value)
        {
            if (Endianness == Endianness.BigEndian)
            {
                value = EndianUtil.SwapEndian(value);
            }
            Stream.Write(BitConverter.GetBytes(value), 0, 4);
        }

        public void WriteString(string value, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (i < value.Length)
                {
                    Stream.WriteByte((byte)value[i]);
                }
                else
                {
                    Stream.WriteByte(0x0);
                }
            }
        }
    }
}
