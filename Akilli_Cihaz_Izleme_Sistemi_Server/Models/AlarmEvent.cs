namespace Akilli_Cihaz_Izleme_Sistemi_Server.Models
{
    public class AlarmEvent
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string Message { get; set; }
        public DateTime OccuredAt { get; set; }
    }
}