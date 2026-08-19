// İstemciden bir cihaza gönderilen komutu taşıyan bir sınıf

namespace Akilli_Cihaz_Izleme_Sistemi_Server.Models
{
    public class DeviceCommand
    {
        public string CommandType { get; set; }

        public double? Value { get; set; }
    }
}