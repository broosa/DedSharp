using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DedSharp
{
    internal class IcpPacket
    {
        public byte OpType = 0;
        public byte SequenceNum = 0;
        public byte[] PacketBuffer;
        public byte PacketLength
        {
            get
            {
                return (byte) PacketBuffer.Length;
            }
        }

        public byte[] GetBytes()
        {
            var outputBuf = new byte[64];

            //HID Report ID = 0xf0
            outputBuf[0] = 0xf0;
            outputBuf[1] = OpType;
            outputBuf[2] = SequenceNum;
            outputBuf[3] = PacketLength;
            PacketBuffer.CopyTo(outputBuf, 4);

            return outputBuf;
        }
    }
}
