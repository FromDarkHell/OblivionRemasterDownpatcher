using LiveCharts;
using LiveCharts.Wpf;
using OblivionRemasterDownpatcher.Util;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace OblivionRemasterDownpatcher.Components
{
    /// <summary>
    /// Interaction logic for NetworkGraph.xaml
    /// </summary>
    public partial class NetworkGraph : UserControl, INotifyPropertyChanged
    {
        private Timer _timer;
        private NetworkMonitor? _monitor = null;
        private ChartValues<double> _downloadSpeeds = new();
        public SeriesCollection SeriesCollection { get; set; }
        public AxesCollection AxisX { get; set; }
        public AxesCollection AxisY { get; set; }

        public bool MonitorInitialized { get; set; } = false;
        public bool MonitorUninitialized { get { return !MonitorInitialized; } }

        public NetworkGraph()
        {
            DataContext = this;
            for(int i = 0; i < 60; i++) _downloadSpeeds.Add(0);

            SeriesCollection = new SeriesCollection()
            {
                new LineSeries { Title = "Download Speed", Values = _downloadSpeeds, LineSmoothness = 0.3, PointGeometry = null }
            };
            AxisX = new AxesCollection()
            {
                new Axis { Title = "Time (s)", ShowLabels = false }
            };

            AxisY = new AxesCollection() { new Axis { Title = "Mbps", LabelFormatter = value => $"{value:F1} Mbps", MinValue = 0 } };

            Dispatcher.BeginInvoke(() =>
            {
                _monitor = new NetworkMonitor();
                _timer = new Timer(UpdateSpeed, null, 0, 1000);
                MonitorInitialized = true;

                OnPropertyChanged(nameof(MonitorInitialized));
                OnPropertyChanged(nameof(MonitorUninitialized));
            });

            InitializeComponent();
        }

        private void UpdateSpeed(object? state)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var speed = _monitor?.GetDownloadSpeedMbps() ?? 0;

                if (_downloadSpeeds.Count >= 60)
                    _downloadSpeeds.RemoveAt(0); // keep it rolling at 60 points

                _downloadSpeeds.Add(speed);
                OnPropertyChanged(nameof(_downloadSpeeds));
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
