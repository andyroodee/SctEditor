namespace SctEditor
{
    public class EndianUtil
    {
        // Swap endianness of a 16-bit unsigned integer
        public static ushort SwapEndian(ushort x)
        {
            return (ushort)((x << 8) | (x >> 8));
        }

        // Swap endianness of a 32-bit unsigned integer
        public static uint SwapEndian(uint x)
        {
            return (x >> 24) |
                ((x << 8) & 0x00FF0000) |
                ((x >> 8) & 0x0000FF00) |
                (x << 24);
        }

        public static void SwapBytes(ref byte a, ref byte b)
        {
            byte temp = a;
            a = b;
            b = temp;
        }

        public static void ShiftBits(ref byte x)
        {
            // Turns AABBCCDD into DDCCBBAA
            byte a = (byte)((x & 0xC0) >> 6);
            byte b = (byte)((x & 0x30) >> 4);
            byte c = (byte)((x & 0x0C) >> 2);
            byte d = (byte)(x & 0x03);
            x = (byte)((d << 6) | (c << 4) | (b << 2) | a);
        }        
    };
}