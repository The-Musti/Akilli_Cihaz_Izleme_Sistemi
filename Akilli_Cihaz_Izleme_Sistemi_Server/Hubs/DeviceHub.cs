using Microsoft.AspNetCore.SignalR;
using Akilli_Cihaz_Izleme_Sistemi_Server.Repository;


// İstemci, Program.cs'de tanımlanan "/hud/devices" adresine bağlanır,
// bu Hub üzerinden server ile bağlantı kurar.
// DevicesController ve DeviceSimulationService bu Hub üzerinden
// istemcilere mesaj yayınlar.
namespace Akilli_Cihaz_Izleme_Sistemi_Server.Hubs
{
    public class DeviceHub : Hub
    {
        private readonly DeviceRepository _repository;

        public DeviceHub(DeviceRepository repository)
        {
            _repository = repository;
        }

        // Bir cihaz SignalR bağlantısı kurduğu anda otomatik olarak tetiklenir.
        public override async Task OnConnectedAsync()
        {

            // O an bütün cihazların güncel listesini database'den çeker.
            var initialDevices = _repository.GetAll();

            // Yukarıda çekilen listeyi sadece yeni bağlanan istemciye gönderir.
            await Clients.Caller.SendAsync("ReceiveInitialDevices", initialDevices);

            await base.OnConnectedAsync();
        }
    }
}