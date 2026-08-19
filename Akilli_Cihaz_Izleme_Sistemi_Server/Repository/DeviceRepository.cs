using Akilli_Cihaz_Izleme_Sistemi_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Akilli_Cihaz_Izleme_Sistemi_Server.Repository
{
    public class Device
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsOn { get; set; }
        public double Value { get; set; }
        public double Esik { get; set; }
    }

    public class DeviceRepository
    {
        private readonly AppDbContext _db;

        public DeviceRepository(AppDbContext db)
        {
            _db = db;
        }

        // Program başlangıcında bir kere çağrılır, mevcut verileri temizleyip seed ile doldurur.
        public static void ResetToSeed(AppDbContext db)
        {
            db.Devices.RemoveRange(db.Devices);
            db.Users.RemoveRange(db.Users);
            db.SaveChanges();
        }

        public IEnumerable<Device> GetAll() => _db.Devices.AsNoTracking().ToList();

        public Device? GetById(int id) => _db.Devices.AsNoTracking().FirstOrDefault(d => d.Id == id);

        // Takip edilen entity döner.
        public Device? GetTrackedById(int id) => _db.Devices.FirstOrDefault(d => d.Id == id);

        public void Save() => _db.SaveChanges();

        public User? ValidateUser(string username, string password) =>
            _db.Users.AsNoTracking().FirstOrDefault(u => u.Username == username && u.Password == password);

        public enum DeviceType
        {
            Lighting,
            HVAC,
            Fan,
            Sensor
        }
        public enum DeviceStatus
        {
            Online,
            Offline,
            Fault
        }

        // Bir cihaza gönderilen kodu işler ve database'e kaydeder.
        // Dönüş değeri "tuple", bu sayede tek bir metod hem başarı/hata durumunu
        // hem de olası alarmları bildirebiliyor.
        public (bool Success, string Message, AlarmEvent? Alarm) ExecuteCommand(int deviceId, DeviceCommand command)
        {

            // Değişiklik yapılacağı için EF Core'un takip ettiği entity isteniyor.
            var device = GetTrackedById(deviceId);
            if (device == null)
                return (false, "Cihaz bulunamadı.", null);

            AlarmEvent? generatedAlarm = null;

            switch (command.CommandType)
            {
                case "TurnOn":
                    device.IsOn = true;
                    device.Status = "Online";
                    break;

                case "TurnOff":
                    device.IsOn = false;
                    device.Status = "Offline";
                    break;

                case "SetValue":
                    
                    // Value nullable olduğu için, bir değer girilirse işlem yapılır.
                    if (command.Value.HasValue)
                    {
                        device.Value = command.Value.Value;

                        // Yeni değer, eşik değerinden büyükse alarm kaydı oluşturur.
                        if (device.Value > device.Esik)
                        {
                            generatedAlarm = new AlarmEvent
                            {
                                DeviceId = device.Id,
                                DeviceName = device.Name,
                                Message = $"{device.Name} - Cihaz eşiği aşıldı! ({device.Value})",
                                OccuredAt = DateTime.UtcNow
                            };
                        }
                    }
                    break;

                default:
                    return (false, "Geçersiz komut türü.", null);
            }

            _db.SaveChanges();

            return (true, $"'{command.CommandType}' komutu başarıyla uygulandı.", generatedAlarm);
        }
    }
}