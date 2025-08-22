namespace DedSharp.BmsDedClientCli
{
    internal class Program
    {
        private static TimeSpan updatePeriod = TimeSpan.FromMilliseconds(100);

        private static string[] blankDisplay = new string[5] {
            "                        ",
            "                        ",
            "                        ",
            "                        ",
            "                        "
        };

        private static string[] waitingDisplay = new string[5] {
            "                        ",
            "                        ",
            "  \x02WAITING FOR BMS...\x02  ",
            "                        ",
            "                        "
        };

        private static string[] waitingDisplayInvert = new string[5] {
            "                        ",
            "                        ",
            "  X                  X  ",
            "                        ",
            "                        "
        };

        static List<DedCommand> GenerateDrawCommands(IDedDisplayProvider displayProvider)
        {
            var commandBuffer = new byte[25 * 65 + 4];
            for (var row = 0; row < 65; row++)
            {
                for (var col = 0; col < 25; col++)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        var pixelValue = displayProvider.IsPixelOn(row, 8 * col + i) ? 1 : 0;
                        commandBuffer[4 + row * 25 + col] |= (byte)(pixelValue << i);
                    }
                }
            }

            var displayMemCommand = new DedCommand() { DataBuffer = commandBuffer, CommandType = DedCommand.CMD_WRITE_DISPLAY_MEM, TimeStamp = 0xffff };
            var refreshCommand = new DedCommand() { DataBuffer = new byte[] { 0 }, CommandType = DedCommand.CMD_REFRESH_DISPLAY, TimeStamp = 0xffff };

            return new List<DedCommand> { displayMemCommand, refreshCommand };
        }

        static void RefreshDisplay(IDedDisplayProvider displayProvider, IcpHidDevice icpDevice)
        {
            var dedCommands = GenerateDrawCommands(displayProvider);
            icpDevice.WriteDedCommands(dedCommands);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Starting HID Device.");

            DedDevice dedDevice = null;
            try
            {
                dedDevice = new DedDevice();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception while connecting to HID device: {e}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Creating BMS Display.");
            BmsDedDisplayProvider bmsDisplayProvider = new BmsDedDisplayProvider();

            Console.WriteLine("Connecting to BMS Shared Memory.");

            var sharedMemReader = new F4SharedMem.Reader();

            while (true)
            {
                //Clear the display
                dedDevice.ClearDisplay();

                var invertMessage = false;

                //Display a message while falcon isn't running
                while (!sharedMemReader.IsFalconRunning)
                {
                    var invertedLines = invertMessage ? waitingDisplayInvert : blankDisplay;

                    bmsDisplayProvider.UpdateDedLines(waitingDisplay, invertedLines);
                    dedDevice.UpdateDisplay(bmsDisplayProvider);

                    invertMessage = !invertMessage;
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                Console.WriteLine("Falcon has started. Beginning display updates.");

                while (true)
                {
                    var updateStartTime = DateTime.Now;
                    var bmsData = sharedMemReader.GetCurrentData();
                    bmsDisplayProvider.UpdateDedLines(bmsData.DEDLines, bmsData.Invert);

                    dedDevice.UpdateDisplay(bmsDisplayProvider);

                    var updateDuration = DateTime.Now - updateStartTime;

                    if (!sharedMemReader.IsConnected)
                    {
                        break;
                    }

                    if (updateDuration < updatePeriod)
                    {
                        Thread.Sleep(updatePeriod - updateDuration);
                    }

                }
            }
        }
    }
}
