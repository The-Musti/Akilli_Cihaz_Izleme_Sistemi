// DeviceHistory tablosuna denk gelen sınıf

namespace Akilli_Cihaz_Izleme_Sistemi_Server.Models
{
    public class DeviceHistory
    {
        public  int Id { get; set; }
        public int DeviceId { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
