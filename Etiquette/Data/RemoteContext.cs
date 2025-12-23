using Etiquette.Models;
using Etiquette.Services;
using Microsoft.EntityFrameworkCore;
using System;

namespace Etiquette.Data
{
    public class RemoteDbContext : DbContext
    {
        public DbSet<ProductLabel> Labels { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Récupération des paramètres (avec valeurs par défaut si vide)
            string server = string.IsNullOrWhiteSpace(AppSettings.DbServer) ? "127.0.0.1" : AppSettings.DbServer;
            string database = string.IsNullOrWhiteSpace(AppSettings.DbName) ? "labelmaster" : AppSettings.DbName;
            string user = string.IsNullOrWhiteSpace(AppSettings.DbUser) ? "root" : AppSettings.DbUser;
            string password = AppSettings.DbPassword ?? "";

            // Gestion du mode SSL (None, Preferred, Required, VerifyCA, VerifyFull)
            string sslMode = string.IsNullOrWhiteSpace(AppSettings.DbSslMode) ? "None" : AppSettings.DbSslMode;

            // Construction de la chaîne de connexion
            var connectionString = $"Server={server};" +
                                   $"Database={database};" +
                                   $"User={user};" +
                                   $"Password={password};" +
                                   $"SslMode={sslMode};";

            try
            {
                // AutoDetect permet de déterminer la version de MariaDB/MySQL automatiquement
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
            catch
            {
                // En cas d'erreur de config ici, cela remontera lors du .CanConnectAsync()
                // On ne fait rien pour ne pas crasher l'app au démarrage
            }
        }
    }
}