using Microsoft.UI.Xaml;
using Etiquette.Services;

namespace Etiquette
{
    public partial class App : Application
    {
        public static NetworkDiscoveryService DiscoveryService { get; private set; }
        public static ConfigurationHttpServer HttpServer { get; private set; }
        public Window m_window;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // LOGIQUE DE DÉMARRAGE DES SERVICES RÉSEAU
            // On ne démarre PAS les serveurs si on est en mode "Standalone" (Monoposte)
            if (AppSettings.AppMode != "Standalone")
            {
                // MultiPoste ou Scanette -> On active le serveur pour la config
                DiscoveryService = new NetworkDiscoveryService();
                DiscoveryService.StartListening();

                HttpServer = new ConfigurationHttpServer();
                HttpServer.Start();
            }

            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}