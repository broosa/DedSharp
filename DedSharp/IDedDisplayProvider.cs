namespace DedSharp
{
    public interface IDedDisplayProvider
    {
        public bool IsPixelOn(int row, int column);
        public bool RowNeedsUpdate(int row);
    }
}
