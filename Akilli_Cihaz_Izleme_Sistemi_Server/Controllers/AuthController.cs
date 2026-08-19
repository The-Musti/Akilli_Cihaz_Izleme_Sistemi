using Microsoft.AspNetCore.Mvc;
using Akilli_Cihaz_Izleme_Sistemi_Server.Repository;

namespace Akilli_Cihaz_Izleme_Sistemi_Server.Controllers
{
    // Kullanıcının girdiği adı ve şifreyi sunucuya iletir.
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Sunucunun login isteğini taşır.
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Username { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {

        // Kullanıcı doğrulamamsı DeviceRepository üzerinden yapılıyor.
        private readonly DeviceRepository _repository;

        public AuthController(DeviceRepository repository)
        {
            _repository = repository;
        }


        // İstemciden gelen kullanıcı adını ve şifresini database içindeki kayıtlarla karşılaştır.
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _repository.ValidateUser(request.Username, request.Password);

            if (user == null)
            {
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "Kullanıcı adı veya şifre hatalı!"
                });
            }

            return Ok(new LoginResponse
            {
                Success = true,
                Message = "Giriş başarılı.",
                Username = user.Username
            });
        }
    }
}