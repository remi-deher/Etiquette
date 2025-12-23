using Etiquette.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace Etiquette.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<ProductLabel> Labels { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Chemin : C:\Users\[Vous]\AppData\Local\Etiquette\etiquette.db
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Etiquette");
            Directory.CreateDirectory(folder); // S'assure que le dossier existe
            var dbPath = Path.Combine(folder, "etiquette.db");

            optionsBuilder.UseSqlite($"Data Source={dbPath}");

        }
    }
}