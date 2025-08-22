using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DedSharp.BmsDedClientGui
{
    /// <summary>
    /// Interaction logic for DedImageMirror.xaml
    /// </summary>
    public partial class DedImageMirror : UserControl, INotifyPropertyChanged
    {
        public class TestDedDisplayProvider : IDedDisplayProvider
        {
            public bool IsPixelOn(int row, int column)
            {
                return column % 2 == 0;
            }

            public bool RowNeedsUpdate(int row)
            {
                return true;
            }
        }

        private readonly byte[] _offColor = { 0, 0, 0, 255 };
        private readonly byte[] _onColor = { 0, 255, 0, 255 };

        private WriteableBitmap _dedBitmap;

        public event PropertyChangedEventHandler? PropertyChanged;

        public static readonly DependencyProperty DedDisplayProviderProperty = DependencyProperty.Register(
            "DedDisplayProvider", typeof(IDedDisplayProvider), typeof(DedImageMirror), new PropertyMetadata(new TestDedDisplayProvider(), OnDedDisplayProviderPropertyChanged));

        public IDedDisplayProvider DedDisplayProvider
        {
            get
            {
                return (IDedDisplayProvider)GetValue(DedDisplayProviderProperty);
            }
            set
            {
                SetValue(DedDisplayProviderProperty, value);
                OnPropertyChanged();
                UpdateDisplay();
            }
        }

        public WriteableBitmap DedBitmap
        {
            get
            {
                return _dedBitmap;
            }
            set
            {
                _dedBitmap = value;
                Debug.WriteLine("DedBitmap Updated.");
                OnPropertyChanged();
                UpdateDisplay();
            }
        }

        public void UpdateDisplay()
        {
            if (DedDisplayProvider == null)
            {
                return;
            }

            byte[] pixels = new byte[200 * 65 * 4];

            for (int row = 0; row < 65; row++)
            {
                for (int col = 0; col < 200; col++)
                {
                    var pixelStart = 4 * (200 * row + col);
                    var color = DedDisplayProvider.IsPixelOn(row, col) ? _onColor : _offColor;
                    for (var j = 0; j < 4; j++)
                    {
                        pixels[pixelStart + j] = color[j];
                    }
                }
            }

            DedBitmap.WritePixels(new Int32Rect(2, 2, 200, 65), pixels, 800, 0);
        }

        public DedImageMirror()
        {
            DedBitmap = new WriteableBitmap(204, 69, 300, 300, PixelFormats.Bgra32, null);
            InitializeComponent();
            Debug.WriteLine("DedImageMirror Initialized");
        }

        private static void OnDedDisplayProviderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine("Dependency object changed");
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
