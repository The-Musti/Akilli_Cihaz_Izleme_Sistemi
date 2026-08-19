using Akilli_Cihaz_Izleme_Sistemi_Client.Models;
using Akilli_Cihaz_Izleme_Sistemi_Client.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Akilli_Cihaz_Izleme_Sistemi_Client.ViewModels
{

    // Ana ekranın tüm verisini ve mantığını yöentir.
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DeviceService _deviceService;

        // XAML'deki CartsianChart bu koleksiyona bağlı
        public ObservableCollection<DateTimePoint> ChartValues { get; } = new ObservableCollection<DateTimePoint>();

        public ObservableCollection<ISeries> Series { get; set; }
        public ObservableCollection<ICartesianAxis> XAxes { get; set; }
        public ObservableCollection<ICartesianAxis> YAxes { get; set; }


        // Son 1 dakikalık veriyi tutan kuyruk.
        private readonly Queue<KeyValuePair<DateTime, double>> _deviceHistory = new Queue<KeyValuePair<DateTime, double>>();

        // Her saniye grafiğe o anki değeri ekler.
        private readonly DispatcherTimer _chartTimer;

        private Device _selectedDevice;
        public Device SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice == value) return;

                _selectedDevice = value;
                OnPropertyChanged(nameof(SelectedDevice));

                // Cihaz değiştiğinde sunucudaki son 1 dakikalık geçmişi çek
                if (_selectedDevice != null)
                {
                    _ = LoadDeviceHistoryAsync(_selectedDevice.Id);
                }
                else
                {
                    _deviceHistory.Clear();
                    UpdateChart();
                }
            }
        }

        public ObservableCollection<Device> Devices { get; set; } = new ObservableCollection<Device>();
        public ObservableCollection<AlarmEvent> Alarms { get; set; } = new ObservableCollection<AlarmEvent>();

        public ICommand TurnOnCommand { get; }
        public ICommand TurnOffCommand { get; }
        public ICommand SendValueCommand { get; }
        
        // Cihazları bulundukları Zone'a göre gruplu gösterir.
        public ICollectionView GroupedDevices { get; set; }

        private string _newValue = string.Empty;
        public string NewValue
        {
            get => _newValue;
            set
            {
                _newValue = value;
                OnPropertyChanged(nameof(NewValue));
            }
        }

        private string _connectionStatus = "Bağlanıyor...";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set { _connectionStatus = value; OnPropertyChanged(nameof(ConnectionStatus)); }
        }

        private string _statusColor = "Orange";
        public string StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(nameof(StatusColor)); }
        }

        public MainViewModel()
        {
            _deviceService = new DeviceService();
            _deviceService.OnConnectionStatusChanged += HandleConnectionStatusChanged;

            InitializeChart();

            TurnOnCommand = new RelayCommand(async _ => await SendCommandAsync("TurnOn"), _ => SelectedDevice != null);
            TurnOffCommand = new RelayCommand(async _ => await SendCommandAsync("TurnOff"), _ => SelectedDevice != null);

            SendValueCommand = new RelayCommand(async _ =>
            {
                if (double.TryParse(NewValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedValue))
                {
                    await SendCommandAsync("SetValue", parsedValue);
                }
                else if (double.TryParse(NewValue, out double parsedValueTR))
                {
                    await SendCommandAsync("SetValue", parsedValueTR);
                }
            }, _ => SelectedDevice != null && !string.IsNullOrWhiteSpace(NewValue));

            GroupedDevices = CollectionViewSource.GetDefaultView(Devices);
            GroupedDevices.GroupDescriptions.Add(new PropertyGroupDescription("Zone"));

            _deviceService.OnInitialDevicesReceived += HandleInitialDevices;
            _deviceService.OnDeviceUpdated += HandleDeviceUpdated;
            _deviceService.OnAlarmRaised += HandleAlarmRaised;

            
            _chartTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _chartTimer.Tick += (s, e) => SampleCurrentValue();
            _chartTimer.Start();

            _ = StartAsync();
        }


        // LiveCharts'ın Series, XAxes, YAxes nesnelerini kurar.
        private void InitializeChart()
        {
            Series = new ObservableCollection<ISeries>
            {
                new LineSeries<DateTimePoint>
                {
                    Values = ChartValues,
                    Name = "Değer",
                    Stroke = new SolidColorPaint(SKColor.Parse("#0066CC"), 2),
                    Fill = new SolidColorPaint(SKColor.Parse("#200066CC")),
                    GeometryStroke = new SolidColorPaint(SKColor.Parse("#0066CC"), 2),
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometrySize = 6,
                    LineSmoothness = 0
                }
            };

            XAxes = new ObservableCollection<ICartesianAxis>
            {
                new Axis
                {
                    Name = "Zaman",
                    Labeler = value =>
                    {
                        try
                        {
                            return new DateTime((long)value).ToString("HH:mm:ss");
                        }
                        catch
                        {
                            return string.Empty;
                        }
                    },
                    UnitWidth = TimeSpan.FromSeconds(1).Ticks,
                    MinStep = TimeSpan.FromSeconds(1).Ticks,
                    LabelsRotation = 0,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
                }
            };

            YAxes = new ObservableCollection<ICartesianAxis>
            {
                new Axis
                {
                    Name = "Değer",
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
                }
            };
        }

        private async Task StartAsync()
        {
            await _deviceService.StartConnectionAsync();
        }

        // Cihaz seçildiğinde sunucudan son 1 dakikanın verisini doldurur.
        private async Task LoadDeviceHistoryAsync(int deviceId)
        {
            var history = await _deviceService.GetDeviceHistoryAsync(deviceId);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                // İstek beklerken kullanıcı başka cihaza geçtiyse bu eski sonucu uygulamadan çık.
                if (SelectedDevice == null || SelectedDevice.Id != deviceId) return;

                _deviceHistory.Clear();

                foreach (var point in history)
                {
                    // Yerel saate çevirme.
                    var localTime = point.Timestamp.Kind == DateTimeKind.Utc
                        ? point.Timestamp.ToLocalTime()
                        : point.Timestamp;

                    _deviceHistory.Enqueue(new KeyValuePair<DateTime, double>(localTime, point.Value));
                }

                // Sunucuda henüz geçmiş kaydı yoksa anlık değerini tek nokta olarak koy
                if (_deviceHistory.Count == 0 && SelectedDevice != null)
                {
                    _deviceHistory.Enqueue(new KeyValuePair<DateTime, double>(DateTime.Now, SelectedDevice.Value));
                }

                TrimHistory(DateTime.Now);
                UpdateChart();
            });
        }


        // Uygulama açıldığında SignalR'dan gelen ilk cihaz listesini ekrana doldurrur.
        private void HandleInitialDevices(List<Device> initialDevices)
        {
            if (initialDevices == null) return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Devices.Clear();
                foreach (var device in initialDevices)
                {
                    Devices.Add(device);
                }

                CollectionViewSource.GetDefaultView(Devices)?.Refresh();
            });
        }


        // Bir cihaz güncellendiğinde listeyi ve seçili cihazı günceller.
        private void HandleDeviceUpdated(Device updatedDevice)
        {
            if (updatedDevice == null) return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                var existingDevice = Devices.FirstOrDefault(d => d.Id == updatedDevice.Id);
                if (existingDevice != null)
                {
                    existingDevice.UpdateFrom(updatedDevice);
                }

                if (SelectedDevice != null && SelectedDevice.Id == updatedDevice.Id)
                {
                    SelectedDevice.UpdateFrom(updatedDevice);
                    OnPropertyChanged(nameof(SelectedDevice));
                }

                GroupedDevices?.Refresh();
            });
        }


        // Yeni gelen alarmları listeye ekler ve 2.5 saniye vurgulu gösterir.
        private void HandleAlarmRaised(AlarmEvent alarm)
        {
            if (alarm == null) return;

            Application.Current?.Dispatcher.Invoke(async () =>
            {
                alarm.IsNew = true;
                Alarms.Insert(0, alarm);

                await Task.Delay(2500);
                alarm.IsNew = false;
            });
        }

        private void HandleConnectionStatusChanged(string status, string color)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                ConnectionStatus = status;
                StatusColor = color;
            });
        }

        // Seçilen cihazın o anki değerini grafiğe yeni bir nokta olarak ekler.
        private void SampleCurrentValue()
        {
            if (SelectedDevice == null) return;

            var now = DateTime.Now;
            _deviceHistory.Enqueue(new KeyValuePair<DateTime, double>(now, SelectedDevice.Value));

            TrimHistory(now);
            UpdateChart();
        }

        // Son 1 dakikadan eski noktaları kuyruktan temizler.
        private void TrimHistory(DateTime now)
        {
            while (_deviceHistory.Count > 0 && (now - _deviceHistory.Peek().Key).TotalSeconds > 60)
            {
                _deviceHistory.Dequeue();
            }
        }

        // _deviceHistory kuyruğundaki verileri grafiğin gösterdiği koleksiyona aktarır.
        private void UpdateChart()
        {
            ChartValues.Clear();

            foreach (var item in _deviceHistory)
            {
                ChartValues.Add(new DateTimePoint(item.Key, item.Value));
            }
        }

        public async Task SendCommandAsync(string commandType, double? value = null)
        {
            if (SelectedDevice != null)
            {
                await _deviceService.SendCommandAsync(SelectedDevice.Id, commandType, value);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}