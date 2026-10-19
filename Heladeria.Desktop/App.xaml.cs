using Heladeria.Desktop.Services;
using Heladeria.Desktop.ViewModels;
using Heladeria.Desktop.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Heladeria.Desktop
{

    public partial class App : Application
    {
        public ApiClient ApiClient { get; }

        public SessionService SessionService { get; }

        public AuthService AuthService { get; }

        public App()
        {
            ApiClient = new ApiClient();
            SessionService = new SessionService();

            AuthService = new AuthService(
                ApiClient,
                SessionService);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var login = new LoginWindow(
                new LoginViewModel(AuthService));

            MainWindow = login;
            login.Show();
        }
    }

}
