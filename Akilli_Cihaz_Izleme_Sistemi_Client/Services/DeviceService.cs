using Akilli_Cihaz_Izleme_Sistemi_Client.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Akilli_Cihaz_Izleme_Sistemi_Client.Services
{

    // Login isteği/cevabı için taşıyıcı sınıflar
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Username { get; set; }
    }


    // Sunucu ile olan tüm iletişimi tek yerden yöneten servis
    // SignalR + REST API
    public class DeviceService
    {
        // Gerçek zamanlı güncelleme için SignalR bağlantısı
        private readonly HubConnection _hubConnection;

        // REST API istekleri için HTTP istemcisi
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7185")
        };

        public event Action<List<Device>>? OnInitialDevicesReceived;
        public event Action<Device>? OnDeviceUpdated;
        public event Action<AlarmEvent>? OnAlarmRaised;
        public event Action<string, string>? OnConnectionStatusChanged;

        public DeviceService()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7185/hub/devices")
                .WithAutomaticReconnect()
                .Build();

            RegisterSignalREvents();
            RegisterConnectionEvents();
        }


        // Sunucudan gelen SignalR mesajlarını dinler ve ilgili event'i tetikler.
        private void RegisterSignalREvents()
        {
            _hubConnection.On<List<Device>>("ReceiveInitialDevices", devices =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    OnInitialDevicesReceived?.Invoke(devices);
                });
            });

            _hubConnection.On<Device>("DeviceUpdated", device =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    OnDeviceUpdated?.Invoke(device);
                });
            });

            _hubConnection.On<AlarmEvent>("AlarmRaised", alarm =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    OnAlarmRaised?.Invoke(alarm);
                });
            });
        }


        // Bağlantı durumuna göre ekrandaki durum göstergesini güncellenir.
        private void RegisterConnectionEvents()
        {
            _hubConnection.Closed += async (error) =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    OnConnectionStatusChanged?.Invoke("Bağlantı kesildi", "Red");
                });
                await Task.CompletedTask;
            };

            _hubConnection.Reconnecting += async (error) =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    OnConnectionStatusChanged?.Invoke("Yeniden bağlanıyor...", "Orange");
                });
                await Task.CompletedTask;
            };

            _hubConnection.Reconnected += async (connectionId) =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    OnConnectionStatusChanged?.Invoke("Bağlı", "Green");
                });

                var currentDevices = await GetDevicesAsync();
                if (currentDevices != null && currentDevices.Any())
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        OnInitialDevicesReceived?.Invoke(currentDevices);
                    });
                }
            };
        }


        // Uygulama açıldığında sunucuya bağlanmayı dener, sunucu açık değilse
        // bir süre sonra tekrar dener, bağlanana kadar böyle devam eder.
        public async Task StartConnectionAsync()
        {
            if (_hubConnection.State == HubConnectionState.Connected)
                return;

            await Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            OnConnectionStatusChanged?.Invoke("Yeniden bağlanıyor...", "Orange");
                        });

                        // SignalR bağlantısını kurma
                        await _hubConnection.StartAsync();

                        // Bağlantı başarılıysa
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            OnConnectionStatusChanged?.Invoke("Bağlı", "Green");
                        });

                        // Cihaz verilerini çekme
                        var currentDevices = await GetDevicesAsync();
                        if (currentDevices != null && currentDevices.Any())
                        {
                            Application.Current?.Dispatcher.Invoke(() =>
                            {
                                OnInitialDevicesReceived?.Invoke(currentDevices);
                            });
                        }

                        
                        break;
                    }
                    catch
                    {
                        // Sunucu henüz açık değilse
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            OnConnectionStatusChanged?.Invoke("Bağlantı kesildi", "Red");
                        });

                        
                        await Task.Delay(2000);
                    }
                }
            });
        }


        // Güncel cihaz listesini REST API'dan çeekme.
        public async Task<List<Device>?> GetDevicesAsync()
        {
            try
            {
                var response = await _http.GetAsync("/api/devices");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Device>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch
            {
            }
            return null;
        }


        // Bir cihaza komut gönderir.
        public async Task SendCommandAsync(int deviceId, string commandType, double? value = null)
        {
            try
            {
                var command = new DeviceCommand { CommandType = commandType, Value = value };
                await _http.PostAsJsonAsync($"/api/devices/{deviceId}/command", command);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Komut gönderme hatası: {ex.Message}");
            }
        }
        
        // Login isteğini sunucudaki "/api/auth/login" endpoint'ine gönderir.
        // Sunucuya ulaşılamazsa Success=false ve açıklayıcı mesaj döner.
        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            try
            {
                var request = new LoginRequest { Username = username, Password = password };
                var response = await _http.PostAsJsonAsync("/api/auth/login", request);

                var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (body != null)
                {
                    return body;
                }

                return new LoginResponse { Success = false, Message = "Sunucudan geçersiz yanıt alındı." };
            }
            catch (Exception)
            {
                return new LoginResponse { Success = false, Message = "Sunucuya bağlanılamadı. Sunucunun çalıştığından emin olun." };
            }
        }


        // Seçilen cihazın son 1 dakikalık değer geçmişini çekme.
        public async Task<List<DeviceHistoryPoint>> GetDeviceHistoryAsync(int deviceId)
        {
            try
            {
                var response = await _http.GetAsync($"/api/devices/{deviceId}/history");
                if (response.IsSuccessStatusCode)
                {
                    var history = await response.Content.ReadFromJsonAsync<List<DeviceHistoryPoint>>();
                    return history ?? new List<DeviceHistoryPoint>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Geçmiş çekme hatası: {ex.Message}");
            }
            return new List<DeviceHistoryPoint>();
        }
    }
}