using System.Text;
using Extensions;
using System.IO;

namespace SctEditor.Sct
{
    public class DialogItem : SctItem
    {
        private readonly byte[] NamePreamble = new byte[] { 0x5C, 0x68, 0x28, 0x81 }; // The start of a dialog entry name "\h(.
        private readonly byte[] NameEnd = new byte[] { 0x81, 0x74, 0x29 };
        private const byte StartName = 0x73; // Start name delimiter
        private const byte EndName = 0x81; // End name delimiter 
        private const byte NoName = 0x40; // Sometimes there is no name "@"
        private const byte RightParen = 0x29;
        private const byte Space = 0x7F;
        private const byte EscapeSeqStart = 0x5C;
        private const byte Newline = 0x6E;
        private const byte EndMessageE = 0x65;
        private const byte EndMessageC = 0x63;
        private const byte EndMessageA = 0x61;
        private const byte EllipsisStart = 0x81;
        private const byte EllipsisEnd = 0x63;
        private const byte LeftQuote = 0x5B;
        private const byte RightQuote = 0x5D;

        private const byte NameTypeOffset = 0x14; // The name type offset from the start of raw data.
        
        private uint _messageOffset;
        private byte _messageEndType;

        public DialogItem(byte[] rawData) : base(rawData)
        {
            ReadName();
            ReadMessage();
        }

        public string Name { get; set; }

        public string Message { get; set;}
        
        public byte[] ToByteArray()
        {
            byte[] result;
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(Data, 0, ItemPreambleSize);
                WriteNameToStream(ms);
                WriteMessageToStream(ms);
                PadItem(ms);

                result = ms.ToArray();
            }
            return result;
        }

        // Item sizes have to be a multiple of 4, which means we might need to pad with 0 bytes.
        private void PadItem(Stream stream)
        {
            int padSize = (byte)(4 - stream.Length % 4);
            for (int i = 0; i < padSize; i++)
            {
                stream.Write((byte)0);
            }
        }

        private void WriteNameToStream(Stream stream)
        {
            stream.Write(NamePreamble, 0, NamePreamble.Length);
            if (!string.IsNullOrEmpty(Name))
            {
                stream.Write(StartName);
                stream.Write(Encoding.ASCII.GetBytes(Name.Replace(' ', (char)0x7F)));
                stream.Write(NameEnd);
            }
            else
            {
                stream.Write(NoName);
                stream.Write(RightParen);
            }
        }

        private void WriteMessageToStream(Stream stream)
        {
            bool inQuote = false;
            for (int i = 0; i < Message.Length; i++)
            {
                char c = Message[i];
                if (c == ' ')
                {
                    stream.Write(Space);
                }
                else if (c == '\r')
                {
                    stream.Write(EscapeSeqStart);
                    stream.Write(Newline);
                    i++; // skip the \n
                }
                else if (c == '\n')
                {
                    stream.Write(EscapeSeqStart);
                    stream.Write(Newline);
                }
                else if (c == '.')
                {
                    if (i + 2 < Message.Length && Message[i + 1] == '.' && Message[i + 2] == '.')
                    {
                        stream.Write(EllipsisStart);
                        stream.Write(EllipsisEnd);
                        i += 2;
                    }
                    else
                    {
                        stream.Write((byte)c);
                    }
                }
                else if (c == '"')
                {
                    if (inQuote)
                    {
                        stream.Write(RightQuote);                        
                    }
                    else
                    {
                        stream.Write(LeftQuote);
                    }
                    inQuote = !inQuote;
                }
                else
                {
                    // TODO: This might be a good place to check if the character is valid.
                    stream.Write((byte)c);
                }
            }
            // Write out the proper ending character
            stream.Write(EscapeSeqStart);
            stream.Write(_messageEndType);
        }
        
        private void ReadName()
        {
            if (Data[NameTypeOffset] == StartName)
            {
                _messageOffset = NameTypeOffset + 4;
                // Read until you hit the EndName delimiter.
                StringBuilder sb = new StringBuilder();
                for (int offset = NameTypeOffset + 1; Data[offset] != EndName; offset++, _messageOffset++)
                {
                    if (Data[offset] == Space)
                    {
                        sb.Append(' ');
                    }
                    else
                    {
                        sb.Append((char)Data[offset]);
                    }
                }
                Name = sb.ToString();
            }
            else
            {
                _messageOffset = NameTypeOffset + 2;
                Name = string.Empty;
            }
        }

        private void ReadMessage()
        {
            // Find the start of the message (after the name).
            StringBuilder sb = new StringBuilder();
            uint offset = _messageOffset;
            while (true)
            {
                byte b = Data[offset];
                if (b == EscapeSeqStart)
                {
                    byte next = Data[offset + 1];
                    if (next == Newline)
                    {
                        sb.AppendLine();
                    }
                    else if (next == EndMessageE || next == EndMessageC || next == EndMessageA)
                    {
                        _messageEndType = next;
                        break;
                    }
                    offset += 2;
                }
                else if (b == EllipsisStart)
                {
                    byte next = Data[offset + 1];
                    if (next == EllipsisEnd)
                    {
                        sb.Append("...");
                    }
                    offset += 2;
                }
                else if (b == LeftQuote || b == RightQuote)
                {
                    sb.Append('"');
                    offset++;
                }
                else if (b == Space)
                {
                    sb.Append(' ');
                    offset++;
                }
                else if (b == 0)
                {
                    break;
                }
                else
                {
                    sb.Append((char)b);
                    offset++;
                }
            }
            Message = sb.ToString();
        }
    }
}
