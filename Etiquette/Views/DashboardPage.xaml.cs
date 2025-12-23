#nullable enable

using Etiquette.Services;
using Etiquette.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Drawing.Printing;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Etiquette.Views
{
    public sealed partial class DashboardPage : Page
    {
        public MainViewModel ViewModel { get; }

        public DashboardPage()
        {
            this.InitializeComponent();
            ViewModel = new MainViewModel();

            // Abonnement à l'événement d'impression
            ViewModel.PrintRequested += ViewModel_PrintRequested;

            // Ajustement de l'interface au chargement (pour masquer "En attente" si nécessaire)
            this.Loaded += DashboardPage_Loaded;
        }

        private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            AdjustLayoutForMode();
        }

        private void AdjustLayoutForMode()
        {
            // On vérifie si on est en mode Scanette
            // Si AppMode n'est PAS "Scanette" (donc Standalone ou MultiPoste), on cache la liste d'attente
            if (AppSettings.AppMode != "Scanette")
            {
                // On cherche la Grille qui se trouve dans la colonne 1 (Partie Droite)
                if (this.Content is Grid mainGrid)
                {
                    foreach (var child in mainGrid.Children)
                    {
                        // On identifie la Grid de droite par sa position dans la colonne 1
                        if (child is Grid rightGrid && Grid.GetColumn(rightGrid) == 1)
                        {
                            // La structure attendue de cette Grid (définie dans le XAML) est :
                            // Row 0 : InfoBar
                            // Row 1 : Filtres
                            // Row 2 : Liste "En attente" (Celle qu'on veut cacher)
                            // Row 3 : Séparateur
                            // Row 4 : Historique

                            if (rightGrid.RowDefinitions.Count > 3)
                            {
                                // On réduit la hauteur à 0 pour faire disparaître la section
                                rightGrid.RowDefinitions[2].Height = new GridLength(0);
                                rightGrid.RowDefinitions[3].Height = new GridLength(0);
                            }
                            break;
                        }
                    }
                }
            }
        }

        // Action à effectuer quand l'impression est demandée par le ViewModel
        private async void ViewModel_PrintRequested(object? _, EventArgs e)
        {
            try
            {
                ViewModel.UpdateStatus("Préparation...", InfoBarSeverity.Informational);

                // 1. Capture de l'élément visuel
                using var imageStream = await CaptureElementToStreamAsync(LabelPreviewContainer);

                if (imageStream == null)
                {
                    ViewModel.UpdateStatus("Erreur de capture visuelle.", InfoBarSeverity.Error);
                    return;
                }

                // 2. Configuration de l'imprimante
                PrintDocument pd = new();
                string printerName = AppSettings.PrinterName;

                if (!string.IsNullOrEmpty(printerName))
                {
                    pd.PrinterSettings.PrinterName = printerName;

                    if (!pd.PrinterSettings.IsValid)
                    {
                        ViewModel.UpdateStatus($"Imprimante introuvable : {printerName}", InfoBarSeverity.Error);
                        return;
                    }
                }

                // 3. Dessin de la page
                pd.PrintPage += (s, args) =>
                {
                    using var bitmap = System.Drawing.Image.FromStream(imageStream);
                    var bounds = args.PageBounds;

                    float ratioX = (float)bounds.Width / bitmap.Width;
                    float ratioY = (float)bounds.Height / bitmap.Height;
                    float ratio = Math.Min(ratioX, ratioY);

                    float newWidth = bitmap.Width * ratio;
                    float newHeight = bitmap.Height * ratio;

                    float posX = bounds.Left + (bounds.Width - newWidth) / 2;
                    float posY = bounds.Top + (bounds.Height - newHeight) / 2;

                    if (args.Graphics != null)
                    {
                        args.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        args.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        args.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        args.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                        args.Graphics.DrawImage(bitmap, posX, posY, newWidth, newHeight);
                    }
                };

                ViewModel.UpdateStatus($"Envoi vers {(string.IsNullOrEmpty(printerName) ? "Défaut" : printerName)}...", InfoBarSeverity.Informational);

                await Task.Run(pd.Print);

                ViewModel.UpdateStatus("Impression terminée !", InfoBarSeverity.Success);

                // --- CHANGEMENT DE LOGIQUE DE FOCUS ---
                // On remet le focus sur le NOM DU PRODUIT pour recommencer le cycle
                ProductNameTextBox.Focus(FocusState.Programmatic);
                // On sélectionne tout le texte pour pouvoir l'écraser directement si on change de produit
                ProductNameTextBox.SelectAll();
            }
            catch (Exception ex)
            {
                ViewModel.UpdateStatus($"Erreur : {ex.Message}", InfoBarSeverity.Error);
            }
        }

        // Gestion touche Entrée sur le NOM du produit -> Passe au Scan
        private void ProductNameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                // Empêche le son "ding" par défaut
                e.Handled = true;
                // Focus sur la zone de scan
                ScanTextBox.Focus(FocusState.Programmatic);
                ScanTextBox.SelectAll();
            }
        }

        // Gestion touche Entrée sur le SCAN -> Lance l'impression
        private void ScanTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (ViewModel.ProcessScanCommand.CanExecute(null))
                {
                    ViewModel.ProcessScanCommand.Execute(null);
                }
            }
        }

        private static async Task<MemoryStream?> CaptureElementToStreamAsync(UIElement element)
        {
            try
            {
                RenderTargetBitmap renderTargetBitmap = new();
                await renderTargetBitmap.RenderAsync(element);

                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                using var stream = new InMemoryRandomAccessStream();
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Ignore,
                    (uint)renderTargetBitmap.PixelWidth,
                    (uint)renderTargetBitmap.PixelHeight,
                    96, 96,
                    pixelBuffer.ToArray());

                await encoder.FlushAsync();

                var memoryStream = new MemoryStream();
                await stream.AsStreamForRead().CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch
            {
                return null;
            }
        }
    }
}