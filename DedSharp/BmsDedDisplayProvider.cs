using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Data;
using System.Diagnostics;

namespace DedSharp
{
    public class BmsDedDisplayProvider : IDedDisplayProvider
    {

        private static readonly uint DISPLAY_WIDTH = 200;
        private static readonly uint DISPLAY_HEIGHT = 65;

        private static readonly uint FONT_CHAR_WIDTH = 8;
        private static readonly uint FONT_CHAR_HEIGHT = 13;

        private static readonly uint DED_ROWS = 5;
        private static readonly uint DED_COLUMNS = 24;

        private static readonly ReaderWriterLock _pixelDataLock = new ReaderWriterLock();

        //This array contains the ascii representation of each character that the font could render.
        //0x00 indicates that the corresponding position in the font doesn't correspond with anything
        //(excepting the last index, which we use to represent characters which we can't render)
        private static readonly byte[] _fontBytes = Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890\x01()<>[]+-\x02/=^|~\x7f.,!?:;&_'\"%#@{} \x7f\x7f\x7f\x7f");

        private Dictionary<byte, bool[]?> _glyphMap;
        private Dictionary<byte, bool[]?> _glyphMapInverted;
        
        private Bitmap _dedFont;
        private Bitmap _dedFontInverted;
        private string[] _dedLines = new string[5];
        private string[] _dedLinesInverted = new string[5];
        private bool[] _linesToUpdate = { true, true, true, true, true };

        private bool[] _pixelStates = new bool[DISPLAY_WIDTH * DISPLAY_HEIGHT];

        private bool[] GetPixelStatesForGlyph(int index, bool inverted)
        {
            var fontData = inverted ? _dedFontInverted : _dedFont;

            var pixelStates = new bool[FONT_CHAR_HEIGHT * FONT_CHAR_WIDTH];

            for (int row = 0; row < FONT_CHAR_HEIGHT; row++)
            {
                for (int col = 0; col < FONT_CHAR_WIDTH; col++)
                {
                    var pixelX = (int) (index * FONT_CHAR_WIDTH + col);
                    var pixelY = row;

                    pixelStates[row * FONT_CHAR_WIDTH + col] = fontData.GetPixel(pixelX, pixelY).GetBrightness() > 0;
                }
            }

            return pixelStates;
        }

        public BmsDedDisplayProvider()
        {
            for (int i = 0; i < 5; i++)
            {
                _dedLines[i] = new string(' ', (int) DED_COLUMNS);
                _dedLinesInverted[i] = new string(' ', (int) DED_COLUMNS);
            }

            using MemoryStream dedFontStream = new MemoryStream(DedSharp.Properties.Resources.DedFont);
            _dedFont = (Bitmap)Bitmap.FromStream(dedFontStream);

            using MemoryStream dedInvertedFontStream = new MemoryStream(DedSharp.Properties.Resources.DedFontInverted);
            _dedFontInverted = (Bitmap)Bitmap.FromStream(dedInvertedFontStream);

            _glyphMap = new Dictionary<byte, bool[]?>();
            _glyphMapInverted = new Dictionary<byte, bool[]?>();


            //Map each ascii byte in the font to a set of boolean values representing the state of the pixels
            //in that glyph.
            for (int i = 0; i < _fontBytes.Length; i++)
            {
                var key = _fontBytes[i];

                if (key != 0x7f) { 

                    var glyphPixelValues = GetPixelStatesForGlyph(i, false);
                    var glyphPixelValuesInverted = GetPixelStatesForGlyph(i, true);

                    _glyphMap.Add(key, glyphPixelValues);
                    _glyphMapInverted.Add(key, glyphPixelValuesInverted);

                    //The "*" symbol can be displayed as '*' or \x02
                    if (key == (byte) 0x02)
                    {
                        _glyphMap.Add((byte)'*', glyphPixelValues);
                        _glyphMapInverted.Add((byte)'*', glyphPixelValuesInverted);
                    }
                }
            }

            //Add glyph to be used if we dont' recognize a character.
            _glyphMap.Add(0x7f, GetPixelStatesForGlyph(_fontBytes.Length - 1, false));
            _glyphMapInverted.Add(0x7f, GetPixelStatesForGlyph(_fontBytes.Length - 1, true));
        }

        private void UpdatePixelState(uint row, uint col, bool state)
        {
            _pixelStates[DISPLAY_WIDTH * row + col] = state;
        }

        private void UpdateLinePixelStates(uint lineIndex)
        {
            var lineBytes = Encoding.ASCII.GetBytes(_dedLines[lineIndex]);
            var invertedBytes = Encoding.ASCII.GetBytes(_dedLinesInverted[lineIndex]);

            var startRow = (DISPLAY_HEIGHT - FONT_CHAR_HEIGHT * DED_ROWS) / 2 + lineIndex * FONT_CHAR_HEIGHT;
            var startCol = (DISPLAY_WIDTH - FONT_CHAR_WIDTH * DED_COLUMNS) / 2;

            //var startCol = 0;

            for (int i = 0; i < DED_COLUMNS; i++)
            {
                var glyphStartRow = startRow;
                var glyphStartCol = startCol + i * FONT_CHAR_WIDTH;

                //If the requested line has a non-space at index i, we take our bytes from the inverted font
                var glyphPixelStates = invertedBytes[i] != 0x20 ? _glyphMapInverted.GetValueOrDefault(lineBytes[i], null) : _glyphMap.GetValueOrDefault(lineBytes[i], null);

                //If we couldn't find the byte at lineBytes[i], then substitute our error glyph instead.
                if (glyphPixelStates == null)
                {
                    glyphPixelStates = invertedBytes[i] != 0x20 ? _glyphMapInverted.GetValueOrDefault((byte) 0x7f, null) : _glyphMap.GetValueOrDefault((byte)0x7f, null);
                }

                if (glyphPixelStates == null)
                {
                    //Something weird is going on.
                    return;
                }

                for (int glyphRow = 0; glyphRow < FONT_CHAR_HEIGHT; glyphRow++)
                {
                    for (int glyphCol = 0; glyphCol < FONT_CHAR_WIDTH; glyphCol++)
                    {
                        UpdatePixelState((uint) (glyphStartRow + glyphRow), (uint) (glyphStartCol + glyphCol), glyphPixelStates[glyphRow * FONT_CHAR_WIDTH + glyphCol]);
                    }
                }
            }
        }

        public void UpdateDedLines(string[] newDedLines, string[] invertedDedLines)
        {
            _pixelDataLock.AcquireWriterLock(TimeSpan.FromSeconds(5));
            for (int i = 0; i < newDedLines.Length; i++)
            {
                //If either the current line on the DED or its inversion states have changed,
                //update it and mark its index as dirty.

                if (newDedLines[i].Length == 0 || _dedLinesInverted[i].Length == 0)
                {
                    _dedLines[i] = new string(' ', 25);
                    _dedLinesInverted[i] = new string(' ', 25);
                    continue;
                }
                if (newDedLines[i] != _dedLines[i] || invertedDedLines[i] != _dedLinesInverted[i])
                {
                    MarkRowDirty(i, true);
                    _dedLines[i] = newDedLines[i];
                    _dedLinesInverted[i] = invertedDedLines[i];

                    UpdateLinePixelStates((uint) i);
                }
            }
            _pixelDataLock.ReleaseWriterLock();
        }

        public bool IsPixelOn(int row, int col)
        {
            _pixelDataLock.AcquireReaderLock(TimeSpan.FromSeconds(5));
            if (row >= 65 || row < 0 || col >= 200 || col < 0)
            {
                return false;
            }
            _pixelDataLock.ReleaseReaderLock();
            return _pixelStates[200 * row + col];

        }

        public bool RowNeedsUpdate(int row)
        {
            _pixelDataLock.AcquireReaderLock(TimeSpan.FromSeconds(5));
            if (row >= 5 || row < 0)
            {
                return false;
            }
            _pixelDataLock.ReleaseReaderLock();
            return _linesToUpdate[row];
        }

        public void MarkRowDirty(int row, bool isDirty)
        {

        }
    }
}
