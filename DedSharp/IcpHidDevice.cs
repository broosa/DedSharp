using HidSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DedSharp
{
    public class IcpDeviceException : Exception
    {
        public IcpDeviceException(string message) : base(message) { }
    }
    public class IcpHidDevice
    {

        private static readonly ushort ICP_VID = 0x4098;
        private static readonly ushort ICP_PID = 0xbf06;
        private readonly HidDevice Device;
        private readonly HidStream DeviceStream;

        private readonly DateTime DeviceStartTime;

        private byte PacketSeqNumber = 1;

        public IcpHidDevice()
        {
            var devList = DeviceList.Local;
            HidDevice hidDevice = null;
            try
            {
                hidDevice = devList.GetHidDevices(ICP_VID, ICP_PID).First();

                if (hidDevice == null)
                {
                    throw new IcpDeviceException("ICP USB HID Device not found.");
                }
            } 
            catch (InvalidOperationException ex)
            {
                throw new IcpDeviceException("ICP USB HID Device not found.");
            }


            Device = hidDevice;

            var config = new OpenConfiguration();
            config.SetOption(OpenOption.Exclusive, true);
            config.SetOption(OpenOption.Interruptible, false);
            DeviceStream = Device.Open(config);

            var streamOpened = true;
            if (!streamOpened) {
                throw new IcpDeviceException("Could not open HID device stream.");
            }

            WriteIcpPacket(new IcpPacket() { OpType = 0x02, PacketBuffer = new byte[] { 0 } });

            DeviceStartTime = DateTime.Now;
        }

        private byte[] CommandListAsBytes(List<DedCommand> commands)
        {
            List<byte> commandBytes = new List<byte>();

            foreach (var command in commands)
            {
                foreach (var b in command.GetBytes())
                {
                    commandBytes.Add(b);
                }
            }

            return commandBytes.ToArray();
        }

        private void WriteIcpPacket(IcpPacket packet)
        {
            DeviceStream.Write(packet.GetBytes());
        }

        private async Task WriteIcpPacketAsync(IcpPacket packet)
        {
            await DeviceStream.WriteAsync(packet.GetBytes());
        }

        private void WriteCommandBytes(byte[] commandBytes)
        {
            for (var packetStartIndex = 0; packetStartIndex < commandBytes.Length; packetStartIndex += 60)
            {
                var packetStopIndex = commandBytes.Length - packetStartIndex >= 60 ? packetStartIndex + 60 : commandBytes.Length;

                var packet = new IcpPacket
                {
                    SequenceNum = PacketSeqNumber++,
                    PacketBuffer = commandBytes[packetStartIndex..packetStopIndex]
                };

                WriteIcpPacket(packet);
            }
        }

        private async Task WriteCommandBytesAsync(byte[] commandBytes)
        {
            for (var packetStartIndex = 0; packetStartIndex < commandBytes.Length; packetStartIndex += 60)
            {
                var packetStopIndex = commandBytes.Length - packetStartIndex >= 60 ? packetStartIndex + 60 : commandBytes.Length;
                var packet = new IcpPacket
                {
                    SequenceNum = PacketSeqNumber++,
                    PacketBuffer = commandBytes[packetStartIndex..packetStopIndex]
                };

                await WriteIcpPacketAsync(packet);
            }
        }

        public void WriteDedCommands(List<DedCommand> commands)
        {
            var commandBytes = CommandListAsBytes(commands);

            WriteCommandBytes(commandBytes);
        }

        public async Task WriteDedCommandsAsync(List<DedCommand> commands)
        {
            var commandBytes = CommandListAsBytes(commands);
            await WriteCommandBytesAsync(commandBytes);
        }

        public void Dispose() => DeviceStream.Dispose();
    }
}
