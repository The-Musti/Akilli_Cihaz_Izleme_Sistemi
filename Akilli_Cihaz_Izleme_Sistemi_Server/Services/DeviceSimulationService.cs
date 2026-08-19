using Microsoft.AspNetCore.SignalR;
using Akilli_Cihaz_Izleme_Sistemi_Server.Hubs;
using Akilli_Cihaz_Izleme_Sistemi_Server.Models;
using Akilli_Cihaz_Izleme_Sistemi_Server.Repository;


// Arka planda değer güncelleme simülasyonu

namespace Akilli_Cihaz_Izleme_Sistemi_Server.Services
{
    public class DeviceSimulationService : BackgroundService
    {

        // DeviceSimulationService bir BackgroundService (singleton) olduğu için,
        // scoped olarak kayıtlı DeviceRepository/AppDbContext'i doğrudan constructor'dan alamaz.
        // IServiceScopeFactory ile her çalışma turunda kısa ömürlü
        // bir scope açılır, ihtiyaç duyulan servisler o scope'tan alınır ve iş bitince kapatılır.
        private readonly IServiceScopeFactory _scopeFactory;

        // SignalR üzerinden tüm bağlı istemcilere mesaj göndermek için
        private readonly IHubContext<DeviceHub> _hubContext;

        // Simülsyonun rastgele bir cihaz seçmesi ve değerini rastgele değiştirmesi için
        private readonly Random _random = new();

        public DeviceSimulationService(IServiceScopeFactory scopeFactory, IHubContext<DeviceHub> hubContext)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // 5 saniyede bir rastgele cihaz seçimi
                await Task.Delay(5000, stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var repository = scope.ServiceProvider.GetRequiredService<DeviceRepository>();

                var devices = repository.GetAll().ToList();
                if (!devices.Any()) continue;

                var now = DateTime.UtcNow;

                // Açık olan rastgele bir cihazın değerini güncellemesi
                var activeDevices = devices.Where(d => d.IsOn).ToList();
                if (activeDevices.Any())
                {
                    var selectedDevice = activeDevices[_random.Next(activeDevices.Count)];
                    double change = (_random.NextDouble() * 6.0) - 3.0;
                    var newValue = Math.Max(0, Math.Round(selectedDevice.Value + change, 1));

                    var (success, _, alarm) = repository.ExecuteCommand(selectedDevice.Id, new DeviceCommand
                    {
                        CommandType = "SetValue",
                        Value = newValue
                    });

                    if (success)
                    {
                        var updatedDevice = repository.GetById(selectedDevice.Id);
                        await _hubContext.Clients.All.SendAsync("DeviceUpdated", updatedDevice, stoppingToken);

                        if (alarm != null)
                        {
                            await _hubContext.Clients.All.SendAsync("AlarmRaised", alarm, stoppingToken);
                        }
                    }
                }

                // Bütün cihazların güncel değerleri geçmiş tablosuna yazılır
                var currentDevices = repository.GetAll().ToList();
                foreach (var dev in currentDevices)
                {
                    db.DeviceHistories.Add(new DeviceHistory
                    {
                        DeviceId = dev.Id,
                        Value = dev.Value,
                        Timestamp = now
                    });
                }

                await db.SaveChangesAsync(stoppingToken);
            }
        }
    }
}