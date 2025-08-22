namespace DedSharp
{
    public class DedDevice : IDisposable
    {
        public class DeviceException : Exception
        {
            public DeviceException(string message) : base(message) { }
        }
        //Utility class for blanking the display on request.
        private class BlankDisplayProvider : IDedDisplayProvider
        {
            public bool IsPixelOn(int row, int column)
            {
                return false;
            }

            public bool RowNeedsUpdate(int row)
            {
                return true;
            }
        }

        private static BlankDisplayProvider _blankDisplayProvider = new BlankDisplayProvider();

        private static List<DedCommand> _blankDrawCommands = _generateDrawCommands(_blankDisplayProvider);

        private static readonly uint DISPLAY_WIDTH = 200;
        private static readonly uint DISPLAY_HEIGHT = 65;

        private IcpHidDevice _hidDevice;

        public DedDevice()
        {
            try
            {
                _hidDevice = new IcpHidDevice();
            }
            catch (IcpDeviceException ex)
            {
                throw new DeviceException($"HID device initialization exception: {ex}");
            }
            catch (Exception ex)
            {
                throw new DeviceException($"Unhandled exception during device initialization: {ex}");
            }
        }

        private static List<DedCommand> _generateDrawCommands(IDedDisplayProvider displayProvider)
        {
            var commandBuffer = new byte[(DISPLAY_HEIGHT * DISPLAY_WIDTH / 8) + 4];
            for (var row = 0; row < DISPLAY_HEIGHT; row++)
            {
                for (var col = 0; col < DISPLAY_WIDTH / 8; col++)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        var pixelValue = displayProvider.IsPixelOn(row, 8 * col + i) ? 1 : 0;
                        commandBuffer[4 + row * (DISPLAY_WIDTH / 8) + col] |= (byte)(pixelValue << i);
                    }
                }
            }

            var displayMemCommand = new DedCommand() { DataBuffer = commandBuffer, CommandType = DedCommand.CMD_WRITE_DISPLAY_MEM, TimeStamp = 0xffff };
            var refreshCommand = new DedCommand() { DataBuffer = new byte[] { 0 }, CommandType = DedCommand.CMD_REFRESH_DISPLAY, TimeStamp = 0xffff };

            return new List<DedCommand> { displayMemCommand, refreshCommand };
        }

        public void UpdateDisplay(IDedDisplayProvider displayProvider)
        {
            var dedCommands = _generateDrawCommands(displayProvider);
            _hidDevice.WriteDedCommands(dedCommands);
        }

        public async Task UpdateDisplayAsync(IDedDisplayProvider displayProvider)
        {
            var dedCommands = _generateDrawCommands(displayProvider);
            await _hidDevice.WriteDedCommandsAsync(dedCommands);
        }

        public void ClearDisplay()
        {
            _hidDevice.WriteDedCommands(_blankDrawCommands);
        }

        public async Task ClearDisplayAsync()
        {
            await _hidDevice.WriteDedCommandsAsync(_blankDrawCommands);
        }

        public void Dispose()
        {
            _hidDevice?.Dispose();
        }
    }
}
