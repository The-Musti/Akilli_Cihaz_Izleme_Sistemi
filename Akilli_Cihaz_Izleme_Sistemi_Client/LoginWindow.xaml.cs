using System.Windows;
using Akilli_Cihaz_Izleme_Sistemi_Client.Services;

namespace Akilli_Cihaz_Izleme_Sistemi_Client
{
    public partial class LoginWindow : Window
    {
        private readonly DeviceService _deviceService = new DeviceService();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password;

            TxtErrorMessage.Visibility = Visibility.Collapsed;
            BtnLogin.IsEnabled = false;

            try
            {
                var result = await _deviceService.LoginAsync(username, password);

                if (result.Success)
                {
                    // Ana ekranı aç
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();

                    // Giriş penceresini kapat
                    this.Close();
                }
                else
                {
                    // Hatalı girişte ya da sunucu hatasında uyarı göster
                    TxtErrorMessage.Text = result.Message;
                    TxtErrorMessage.Visibility = Visibility.Visible;
                }
            }
            finally
            {
                BtnLogin.IsEnabled = true;
            }
        }
    }
}