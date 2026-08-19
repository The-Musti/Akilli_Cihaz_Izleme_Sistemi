using Akilli_Cihaz_Izleme_Sistemi_Server.Hubs;
using Akilli_Cihaz_Izleme_Sistemi_Server.Models;
using Akilli_Cihaz_Izleme_Sistemi_Server.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace Akilli_Cihaz_Izleme_Sistemi_Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        // Cihazlarla ilgili database işlemlerinin yürütmesini yapar.
        private readonly DeviceRepository _repository;

        // SignalR üzerinden bağlı tüm istemcilere gerçek zamanlı mesaj gönderir.
        private readonly IHubContext<DeviceHub> _hubContext;

        // GetDeviceHistory endpoint'i, DeviceRepository'nin kapsamına girmeyen
        // DeviceHistories tablosuna doğrudan erişmek için
        // AppDbContext'i ayrıca inject eder.
        private readonly AppDbContext _context;

        // Yukarıdaki üç bağımlılığı Program.cs'te yapılan servis kaynaklarından bakarak
        // otomatik olarak sağlar.
        public DevicesController(DeviceRepository repository, IHubContext<DeviceHub> hubContext, AppDbContext context)
        {
            _repository = repository;
            _hubContext = hubContext;
            _context = context;
        }


        [HttpGet]
        public IActionResult GetAllDevices()
        {

            return Ok(_repository.GetAll());
        }

        // Bir cihazın bilgilerini id'ye göre döndürür.
        [HttpGet("{id}")]
        public IActionResult GetDeviceById(int id)
        {
            var device = _repository.GetById(id);
            if (device == null)
            {
                return NotFound(new { Message = $"ID'si {id} olan cihaz bulunamadı." });
            }

            return Ok(device);
        }

        // Bir cihazın son 1 dakikalık değer geçmişini döndürür.
        [HttpGet("{id}/history")]
        public IActionResult GetDeviceHistory(int id)
        {
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);

            var history = _context.DeviceHistories
                .Where(h => h.DeviceId == id && h.Timestamp >= oneMinuteAgo)
                .OrderBy(h => h.Timestamp)
                .Select(h => new
                {
                    h.Value,
                    Timestamp = h.Timestamp.ToString("HH:mm:ss")
                })
                .ToList();

            return Ok(history);
        }

        // Bir cihaza komut gönderir.
        [HttpPost("{id}/command")]
        public async Task<IActionResult> SendCommand(int id, [FromBody] DeviceCommand command)
        {
            var result = _repository.ExecuteCommand(id, command);

            if (!result.Success)
            {
                return result.Message == "Cihaz bulunamadı."
                    ? NotFound(new { result.Message })
                    : BadRequest(new { result.Message });
            }

            // Güncel cihazı çekip clientlara yayınlar.
            var updatedDevice = _repository.GetById(id);
            await _hubContext.Clients.All.SendAsync("DeviceUpdated", updatedDevice);

            // TurnOff komutunda çevrimdışı alarmı üretir.
            if (command.CommandType.Equals("TurnOff", StringComparison.OrdinalIgnoreCase))
            {
                var offlineAlarm = new AlarmEvent
                {
                    DeviceId = updatedDevice!.Id,
                    DeviceName = updatedDevice.Name,
                    Message = $"{updatedDevice.Name} - Cihaz çevrimdışı!",
                    OccuredAt = DateTime.UtcNow
                };

                await _hubContext.Clients.All.SendAsync("AlarmRaised", offlineAlarm);
            }

            if (result.Alarm != null)
            {
                await _hubContext.Clients.All.SendAsync("AlarmRaised", result.Alarm);
            }

            return Ok(updatedDevice);
        }

    }


}