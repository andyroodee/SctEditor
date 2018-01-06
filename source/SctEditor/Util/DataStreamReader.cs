using SctEditor.Sct;
using System;
using System.IO;
using System.Text;

namespace SctEditor.Util
{
    public class DataStreamReader
    {
        private Endianness _endianness;
        private Stream _stream;

        public long StreamPosition
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }
        
        public DataStreamReader(Stream stream, Endianness endianness)
        {
            _stream = stream;
            _endianness = endianness;
        }
        
        public SctItemHeader ReadSctItemHeader(long offset)
        {
            StreamPosition = offset;
            SctItemHeader itemHeader = SctItemHeader.CreateFromStream(this);
            return itemHeader;
        }

        public string ReadString(long offset, int maxLength)
        {
            StreamPosition = offset;

            StringBuilder sb = new StringBuilder(maxLength);

            for (int i = 0; i < maxLength; i++)
            {
                char c = (char)_stream.ReadByte();
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
            _stream.Read(array, 0, 4);
            var value = BitConverter.ToUInt32(array, 0);
            if (_endianness == Endianness.BigEndian)
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
    }
}
