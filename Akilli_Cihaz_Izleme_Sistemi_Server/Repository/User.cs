namespace Akilli_Cihaz_Izleme_Sistemi_Server.Repository
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;

        // Şifre plaintext olarak tutuluyor.
        public string Password { get; set; } = string.Empty;
    }
}