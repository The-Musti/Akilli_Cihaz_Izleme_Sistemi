using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Akilli_Cihaz_Izleme_Sistemi_Client.Models
{
    public class AlarmEvent : INotifyPropertyChanged
    {

        // IsNew ve OccuredAt property'lerin arkasınaki gerçek veriyi tutan alanlar.
        private bool _isNew;
        private DateTime _occuredAt;

        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;


        // Saati yerek saat dilimine çevirme
        public DateTime OccuredAt
        {
            get => _occuredAt.Kind == DateTimeKind.Utc ? _occuredAt.ToLocalTime() : _occuredAt;
            set => _occuredAt = value;
        }


        // Alarm lsiteye eklendiğinde true yapılır, bir süre sonra tekrar false yapılır.
        public bool IsNew
        {
            get => _isNew;
            set
            {
                _isNew = value;
                OnPropertyChanged();
            }
        }


        // WPF binding sistemi bu event'i dinleyerek değişen propety'ni öğrenir.
        public event PropertyChangedEventHandler? PropertyChanged;

        // Bu metodu çağıranın adını otomatik olarak alır.
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}