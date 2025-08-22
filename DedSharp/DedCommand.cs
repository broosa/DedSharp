namespace DedSharp
{
    public class DedCommand
    {

        public static readonly uint CMD_WRITE_DISPLAY_MEM = 0x102;
        public static readonly uint CMD_REFRESH_DISPLAY = 0x103;
        public uint ProductId = 0xbf06;
        public uint CommandType = 0x0103;
        public required uint TimeStamp;
        public required byte[] DataBuffer;

        public byte[] GetBytes()
        {
            var outputBuf = new byte[(sizeof(uint) * 4 + 1) + DataBuffer.Length];
            BitConverter.GetBytes(ProductId).CopyTo(outputBuf, 0);
            BitConverter.GetBytes(CommandType).CopyTo(outputBuf, 4);
            BitConverter.GetBytes(TimeStamp).CopyTo(outputBuf, 8);
            //outputBuf[12] is a zero; skip it.
            BitConverter.GetBytes(DataBuffer.Length).CopyTo(outputBuf, 13);
            DataBuffer.CopyTo(outputBuf, 17);
            return outputBuf;
        }
    }
}
