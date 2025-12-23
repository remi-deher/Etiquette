using System;
using System.Text.Json;
using Etiquette.Models;

namespace Etiquette.Services
{
    public class ConfigurationService
    {
        // Génère le JSON de configuration avec un PIN de sécurité
        public string ExportConfigurationToJson(string pinCode)
        {
            var config = new ConfigurationTransferModel
            {
                DbServer = AppSettings.DbServer,
                DbSslMode = AppSettings.DbSslMode,
                DbName = AppSettings.DbName,
                DbUser = AppSettings.DbUser,
                DbPassword = AppSettings.DbPassword,
                PrinterName = AppSettings.PrinterName,
                SecurityPin = pinCode
            };

            // On sérialise en JSON avec une indentation pour que ce soit lisible (optionnel)
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(config, options);
        }

        // Tente d'importer une configuration JSON
        // Retourne true si succès, false si le PIN est incorrect ou erreur
        public bool ImportConfigurationFromJson(string jsonContent, string userProvidedPin)
        {
            try
            {
                var config = JsonSerializer.Deserialize<ConfigurationTransferModel>(jsonContent);

                if (config == null) return false;

                // VÉRIFICATION DE SÉCURITÉ
                // On vérifie si le PIN saisi par l'utilisateur correspond à celui du fichier
                if (config.SecurityPin != userProvidedPin)
                {
                    return false; // PIN Incorrect, on rejette
                }

                // Application des paramètres
                AppSettings.DbServer = config.DbServer;
                AppSettings.DbSslMode = config.DbSslMode;
                AppSettings.DbName = config.DbName;
                AppSettings.DbUser = config.DbUser;
                AppSettings.DbPassword = config.DbPassword;
                AppSettings.PrinterName = config.PrinterName;

                return true;
            }
            catch (Exception)
            {
                // En cas d'erreur de parsing JSON
                return false;
            }
        }

        // Méthode utilitaire pour générer un PIN aléatoire à 4 chiffres
        public string GenerateRandomPin()
        {
            Random _random = new Random();
            return _random.Next(1000, 9999).ToString();
        }
    }
}