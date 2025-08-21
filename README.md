# DedSharp

## About

DedSharp is a C# Library and associated tools for controlling WinWing replica F-16 ICP/DED hardware.
Included with the software are two Windows programs which can be used to automatically read DED data
from a running instance of Falcon BMS and display the output on the Winwing DED display.

## Installation and Usage with Falcon BMS

1. Grab a copy of the latest release archive and unzip anywhere on your hard drive. 
2. Ensure your ICP/DED is connected over USB and is visible in windows. 
3. Run BmsDedClientGui.exe from the extracted folder. If your hardware is set up correctly,
   DED data should be visible on the hardware ICP/DED once you enter 3D in BMS.

## Acknowledgements

DedSharp makes use of the following tools and libraries:
- [HidSharp](https://www.zer7.com/software/hidsharp) for low-level interaction over the HID protocol.
- A very slightly modified version of [Lightning's Tools](https://github.com/lightningviper/lightningstools) 
  (specifically F4SharedMem) by @lightningviper for reading shared memory data from Falcon BMS. The modified
  version of lightningstools can be found [here](https://github.com/broosa/lightningstools).

## Licensing

This software is licensed under the MIT Software License.