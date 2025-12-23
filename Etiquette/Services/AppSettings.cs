using Windows.Storage;

namespace Etiquette.Services
{
    public static class AppSettings
    {
        private static ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

        // --- GESTION DES MODES ---
        public static string AppMode
        {
            get => LocalSettings.Values["AppMode"] as string ?? "MultiPoste";
            set => LocalSettings.Values["AppMode"] = value;
        }

        public static bool IsFirstRun
        {
            get => (LocalSettings.Values["IsFirstRun"] as bool?) ?? true;
            set => LocalSettings.Values["IsFirstRun"] = value;
        }

        // --- GESTION DES VERSIONS (NOUVEAU) ---
        public static string LastRunVersion
        {
            get => LocalSettings.Values["LastRunVersion"] as string ?? "0.0.0.0";
            set => LocalSettings.Values["LastRunVersion"] = value;
        }

        // --- BASE DE DONNÉES ---
        public static string DbType
        {
            get => LocalSettings.Values["DbType"] as string ?? "MariaDB / MySQL";
            set => LocalSettings.Values["DbType"] = value;
        }

        public static string DbServer
        {
            get => LocalSettings.Values["DbServer"] as string ?? "127.0.0.1";
            set => LocalSettings.Values["DbServer"] = value;
        }

        public static string DbSslMode
        {
            get => LocalSettings.Values["DbSslMode"] as string ?? "None";
            set => LocalSettings.Values["DbSslMode"] = value;
        }

        public static string DbName
        {
            get => LocalSettings.Values["DbName"] as string ?? "labelmaster";
            set => LocalSettings.Values["DbName"] = value;
        }

        public static string DbUser
        {
            get => LocalSettings.Values["DbUser"] as string ?? "root";
            set => LocalSettings.Values["DbUser"] = value;
        }

        public static string DbPassword
        {
            get => LocalSettings.Values["DbPassword"] as string ?? "";
            set => LocalSettings.Values["DbPassword"] = value;
        }

        public static string PrinterName
        {
            get => LocalSettings.Values["PrinterName"] as string ?? "";
            set => LocalSettings.Values["PrinterName"] = value;
        }
    }
}