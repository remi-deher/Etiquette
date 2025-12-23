using System;
using System.ComponentModel.DataAnnotations;

namespace Etiquette.Models
{
    public class ProductLabel
    {
        [Key]
        public int Id { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string BarcodeContent { get; set; } = string.Empty;

        public DateTime ScannedAt { get; set; } = DateTime.Now;

        public string Source { get; set; } = "Local";

        public string Status { get; set; } = "Printed";
    }
}