using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace DedSharp
{

    public class BmsDedGlyph : IEquatable<BmsDedGlyph>
    {
        //This array contains the ascii representation of each character that the font could render.
        //0x00 indicates that the corresponding position in the font doesn't correspond with anything
        //(excepting the last index, which we use to represent characters which we can't render)

        private static readonly byte[] _fontBytes = Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890\x01()<>[]+-\x02/=^|~\x7f.,!?:;&_'\"%#@{} \x7f\x7f\x7f\x7f");

        private static readonly byte FONT_BYTE_GLYPH_NOT_FOUND = 0x7f;
        
        private static readonly int FONT_CHAR_WIDTH = 8;
        private static readonly int FONT_CHAR_HEIGHT = 13;

        private static Bitmap _dedFont = null;
        private static Bitmap _dedFontInverted = null;

        private static Dictionary<byte, bool[]?> _glyphMap = null;
        private static Dictionary<byte, bool[]?> _glyphMapInverted = null;

        public bool[] PixelStates { get; private set; }

        public byte DedByte { get; private set; }

        public bool IsInverted { get; private set; }

        private static bool[] GetPixelStatesAtFontIndex(int index, bool inverted)
        {
            var fontData = inverted ? _dedFontInverted : _dedFont;

            var pixelStates = new bool[FONT_CHAR_HEIGHT * FONT_CHAR_WIDTH];

            for (int row = 0; row < FONT_CHAR_HEIGHT; row++)
            {
                for (int col = 0; col < FONT_CHAR_WIDTH; col++)
                {
                    var pixelX = (int)(index * FONT_CHAR_WIDTH + col);
                    var pixelY = (int) row;

                    pixelStates[GetPixelIndex(row, col)] = fontData.GetPixel(pixelX, pixelY).GetBrightness() > 0;
                }
            }

            return pixelStates;
        }

        public int ColumnCount { get { return FONT_CHAR_WIDTH; } }

        public int RowCount { get { return FONT_CHAR_HEIGHT; } }

        public BmsDedGlyph(byte dedByte, byte invertedByte)
        {
            //Initialize the glyph maps if not already done yet.
            if (_glyphMap == null || _glyphMapInverted == null)
            {
                MemoryStream dedFontStream = new MemoryStream(DedSharp.Properties.Resources.DedFont);
                _dedFont = (Bitmap)Bitmap.FromStream(dedFontStream);

                MemoryStream dedInvertedFontStream = new MemoryStream(DedSharp.Properties.Resources.DedFontInverted);
                _dedFontInverted = (Bitmap)Bitmap.FromStream(dedInvertedFontStream);

                _glyphMap = new Dictionary<byte, bool[]?>();
                _glyphMapInverted = new Dictionary<byte, bool[]?>();


                //Map each ascii byte in the font to a set of boolean values representing the state of the pixels
                //in that glyph.
                for (int i = 0; i < _fontBytes.Length; i++)
                {
                    var key = _fontBytes[i];

                    if (key != FONT_BYTE_GLYPH_NOT_FOUND)
                    {

                        var glyphPixelValues = GetPixelStatesAtFontIndex(i, false);
                        var glyphPixelValuesInverted = GetPixelStatesAtFontIndex(i, true);

                        _glyphMap.Add(key, glyphPixelValues);
                        _glyphMapInverted.Add(key, glyphPixelValuesInverted);

                        //The "*" symbol can be displayed as '*' or \x02
                        if (key == (byte)0x02)
                        {
                            _glyphMap.Add((byte)'*', glyphPixelValues);
                            _glyphMapInverted.Add((byte)'*', glyphPixelValuesInverted);
                        }
                    }
                }

                //Add glyph to be used if we dont' recognize a character.
                _glyphMap.Add(0x7f, GetPixelStatesAtFontIndex(_fontBytes.Length - 1, false));
                _glyphMapInverted.Add(0x7f, GetPixelStatesAtFontIndex(_fontBytes.Length - 1, true));
            }

            //Assign the array of pixel values based on whether the inverted byte is set to a space.
            DedByte = dedByte;
            IsInverted = (invertedByte != (byte)' ');
            var glyphMap = IsInverted ? _glyphMapInverted : _glyphMap;
            if (glyphMap.ContainsKey(DedByte))
            {
                PixelStates = glyphMap[DedByte];
            } 
            else
            {
                PixelStates = glyphMap[FONT_BYTE_GLYPH_NOT_FOUND];
            }
        }

        public static int GetPixelIndex(int row, int col)
        {
            return FONT_CHAR_WIDTH * row + col;
        }

        public bool IsPixelOn(int row, int col)
        {
            return PixelStates[GetPixelIndex(row, col)];
        }

        public bool Equals(BmsDedGlyph? other)
        {
            if (other == null)
            {
                return false;
            }
            return DedByte == other.DedByte && IsInverted == other.IsInverted;
        }
    }

    public class BmsDedDisplayState : IEquatable<BmsDedDisplayState>
    {
        private static readonly int DED_ROWS = 5;
        private static readonly int DED_COLUMNS = 24;

        private BmsDedGlyph[] _glyphs = new BmsDedGlyph[DED_COLUMNS * DED_ROWS];

        public int ColumnCount { get { return DED_COLUMNS; } }
        public int RowCount { get { return DED_ROWS; } }

        public BmsDedDisplayState(string[] dedLines, string[] invertedLines)
        {

            for (int row = 0; row < RowCount; row++)
            {
                var dedLineBytes = Encoding.ASCII.GetBytes(dedLines[row]);
                var invertedLineBytes = Encoding.ASCII.GetBytes(invertedLines[row]);

                for (int col = 0; col < ColumnCount; col++)
                {
                    _glyphs[GetGlyphIndex(row, col)] = new BmsDedGlyph(dedLineBytes[col], invertedLineBytes[col]);
                }
            }
        }
        private int GetGlyphIndex(int row, int col)
        {
            return (int) (row * ColumnCount + col);
        }


        public bool Equals(BmsDedDisplayState? other)
        {
            if (other == null)
            {
                return false;
            }

            for (int row = 0; row < RowCount; row++)
            {
                for (int col = 0; col < ColumnCount; col++)
                {
                    if (!GetGlyphAtPosition(row, col).Equals(other.GetGlyphAtPosition(row, col)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public BmsDedGlyph GetGlyphAtPosition(int row, int column)
        {
            var glyphIndex = GetGlyphIndex(row, column);
            return _glyphs[(int) glyphIndex];
        }

        public BmsDedGlyph GetGlyphAtPixelPosition(int pixelRow, int pixelColumn)
        {
            var glyphRow = pixelRow / RowCount;
            var glyphColumn = pixelColumn / RowCount;

            return GetGlyphAtPosition((int) glyphRow, (int) glyphColumn);
        }

        public bool IsPixelOn(int pixelRow, int pixelColumn)
        {
            var glyph = GetGlyphAtPixelPosition(pixelRow, pixelColumn);

            var pixelRowOffset = pixelRow % RowCount;
            var pixelColumnOffset = pixelColumn % ColumnCount;

            return glyph.IsPixelOn((int) pixelRowOffset, (int) pixelColumnOffset);
        }
    }

    public class BmsDedDisplayProvider : IDedDisplayProvider
    {
        private static readonly uint DISPLAY_WIDTH = 200;
        private static readonly uint DISPLAY_HEIGHT = 65;

        private static readonly ReaderWriterLock _pixelDataLock = new ReaderWriterLock();

        public BmsDedDisplayState DisplayState { get; private set; }

        public BmsDedDisplayProvider()
        {
            DisplayState = null;
        }

        public void UpdateDedLines(string[] newDedLines, string[] invertedDedLines)
        {
            _pixelDataLock.AcquireWriterLock(TimeSpan.FromSeconds(5));
            var newDisplayState = new BmsDedDisplayState(newDedLines, invertedDedLines);

            if (!newDisplayState.Equals(DisplayState))
            {
                DisplayState = newDisplayState;
            }
            _pixelDataLock.ReleaseWriterLock();
        }

        public bool IsPixelOn(int row, int col)
        {
            _pixelDataLock.AcquireReaderLock(TimeSpan.FromSeconds(5));
            if (row >= DISPLAY_HEIGHT || row < 0 || col >= DISPLAY_WIDTH || col < 0)
            {
                return false;
            }
            var pixelState = DisplayState.IsPixelOn(row, col);
            _pixelDataLock.ReleaseReaderLock();
            return pixelState;

        }

        public bool RowNeedsUpdate(int row)
        {
            /*_pixelDataLock.AcquireReaderLock(TimeSpan.FromSeconds(5));
            if (row >= 5 || row < 0)
            {
                return false;
            }
            _pixelDataLock.ReleaseReaderLock();
            return _linesToUpdate[row];*/
            throw new NotImplementedException();
        }

        public void MarkRowDirty(int row, bool isDirty)
        {
            throw new NotImplementedException();
        }
    }
}
