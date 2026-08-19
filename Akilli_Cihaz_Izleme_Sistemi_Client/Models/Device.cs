using System.ComponentModel;
using System.Runtime.CompilerServices;


// İstemci tarafondaki cihaz modeli. Ekrandaki durumlar değiştiğinde arayüz otomatik güncellenir.
namespace Akilli_Cihaz_Izleme_Sistemi_Client.Models
{
    public class Device : INotifyPropertyChanged
    {
        private double _value;
        private bool _isOn;
        private string _status = string.Empty;

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double Esik { get; set; }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); }
        }

        public string StatusColor => Status?.ToLower() switch
        {
            "online" => "Green",
            "fault" => "Red",
            _ => "Gray"
        };

        public bool IsOn
        {
            get => _isOn;
            set { _isOn = value; OnPropertyChanged(); }
        }

        public double Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        // Property adını otomatik alır, arayüze iletir.
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // SignalR'den gelen güncel cihaz verisini mevcut cihaza uygular.
        public void UpdateFrom(Device updated)
        {
            Value = updated.Value;
            IsOn=updated.IsOn;
            Status = updated.Status;

            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(IsOn));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusColor));


        }
    }
}