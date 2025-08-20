using F4SharedMem;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using System.Windows.Threading;

namespace DedSharp.BmsDedClientGui
{
    public class ConnectionBooleanLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var isConnected = (bool)value;
            return isConnected ? "CONNECTED" : "DISCONNECTED";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private bool _isBmsConnected = false;
        private bool _isDedConnected = false;

        public bool IsBmsConnected
        {
            get
            {
                return _isBmsConnected;
            }
            private set
            {
                _isBmsConnected = value;
                OnPropertyChanged();
            }
        }

        public bool IsDedConnected
        {
            get
            {
                return _isDedConnected;
            }
            private set
            {
                _isDedConnected = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer _displayUpdateTimer;

        private System.Timers.Timer _deviceUpdateTimer;

        private DedDevice _dedDevice;

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

        private BmsDedDisplayProvider _dedDisplayProvider;

        public DedDevice DedDevice
        {
            get { return _dedDevice; }
            set
            {
                _dedDevice = value; 
                OnPropertyChanged();    
            }
        }

        public BmsDedDisplayProvider DedDisplayProvider
        {
            get { return _dedDisplayProvider; }
            set { 
                _dedDisplayProvider = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            DedDisplayProvider = new BmsDedDisplayProvider();

            IsDedConnected = false;
            try
            {
                DedDevice = new DedDevice();
                IsDedConnected = true;
            } 
            catch (Exception ex)
            {
                throw ex;
            }

            InitializeComponent();

            _displayUpdateTimer = new DispatcherTimer();

            _displayUpdateTimer.Interval = TimeSpan.FromMilliseconds(100);
            _displayUpdateTimer.Tick += _displayUpdateTimer_Tick;
            _displayUpdateTimer.Start();

            _deviceUpdateTimer = new System.Timers.Timer(TimeSpan.FromMilliseconds(100));
            _deviceUpdateTimer.Elapsed += _deviceUpdateTimer_Elapsed;
            _deviceUpdateTimer.AutoReset = true;
            _deviceUpdateTimer.Enabled = true;

        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void _displayUpdateTimer_Tick(object? sender, EventArgs e)
        {
            Reader bmsSharedMemReader = new Reader();
            if (bmsSharedMemReader.IsFalconRunning)
            {
                if (!IsBmsConnected)
                {
                    IsBmsConnected = true;
                }
                DedMirror.UpdateDisplay();
            } 
            else
            {
                if (IsBmsConnected)
                {
                    IsBmsConnected = false;
                }
            }
        }
        
        //TODO: Better handle changes in DED/BMS connection states.
        private void _deviceUpdateTimer_Elapsed(object? sender, EventArgs e)
        {
            Reader bmsSharedMemReader = new Reader();

            if (bmsSharedMemReader.IsFalconRunning)
            {
                if (!IsBmsConnected)
                {
                    IsBmsConnected = true;
                }
                DedDisplayProvider.UpdateDedLines(bmsSharedMemReader.GetCurrentData().DEDLines, bmsSharedMemReader.GetCurrentData().Invert);

                if (IsDedConnected)
                {
                    DedDevice.UpdateDisplay(DedDisplayProvider);
                }
            }
            else
            {
                if (IsBmsConnected)
                {
                    IsBmsConnected = false;
                }
            }
        }
    }
}