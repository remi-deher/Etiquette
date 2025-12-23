namespace Etiquette.Models
{
    public class ConfigurationTransferModel
    {
        // Données de base de données
        public string DbServer { get; set; } = string.Empty;
        public string DbSslMode { get; set; } = string.Empty;
        public string DbName { get; set; } = string.Empty;
        public string DbUser { get; set; } = string.Empty;
        public string DbPassword { get; set; } = string.Empty;

        // Données d'impression
        public string PrinterName { get; set; } = string.Empty;

        // Sécurité : Le PIN requis pour accepter cette configuration
        public string SecurityPin { get; set; } = string.Empty;
    }
}