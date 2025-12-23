#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Etiquette.Data;
using Etiquette.Models;
using Etiquette.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using QRCoder;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Etiquette.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly DispatcherTimer _syncTimer;
        private bool _isSyncing = false;
        public event EventHandler? PrintRequested;

        // ... (Propriétés InfoBar, ProductName, ScanInput inchangées) ...
        // Je remets les propriétés courtes pour économiser de la place
        private bool _isInfoBarOpen = false; public bool IsInfoBarOpen { get => _isInfoBarOpen; set => SetProperty(ref _isInfoBarOpen, value); }
        private string _infoBarTitle = "Info"; public string InfoBarTitle { get => _infoBarTitle; set => SetProperty(ref _infoBarTitle, value); }
        private string _infoBarMessage = ""; public string InfoBarMessage { get => _infoBarMessage; set => SetProperty(ref _infoBarMessage, value); }
        private InfoBarSeverity _infoBarSeverity = InfoBarSeverity.Informational; public InfoBarSeverity InfoBarSeverity { get => _infoBarSeverity; set => SetProperty(ref _infoBarSeverity, value); }

        private string _productName = ""; public string ProductName { get => _productName; set => SetProperty(ref _productName, value); }
        private string _scanInput = "";
        public string ScanInput { get => _scanInput; set { if (SetProperty(ref _scanInput, value)) GenerateQrPreview(_scanInput); } }
        private BitmapImage? _qrCodeImage; public BitmapImage? QrCodeImage { get => _qrCodeImage; set => SetProperty(ref _qrCodeImage, value); }
        private string _searchText = ""; public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) LoadData(); } }

        public ObservableCollection<ProductLabel> HistoryList { get; } = [];
        public ObservableCollection<ProductLabel> PendingList { get; } = [];

        public MainViewModel()
        {
            _context = new AppDbContext();
            try { _context.Database.EnsureCreated(); } catch (Exception ex) { LoggerService.Current.Log("Err BDD Local", LogLevel.Error, ex); }
            LoadData();

            _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _syncTimer.Tick += async (s, e) => await SyncData();
            _syncTimer.Start();
        }

        [RelayCommand]
        public async Task ProcessScan()
        {
            if (string.IsNullOrWhiteSpace(ScanInput)) return;
            if (string.IsNullOrWhiteSpace(ProductName)) ProductName = "Produit Sans Nom";
            if (ScanInput.StartsWith("http")) { ScanInput = ""; UpdateStatus("URL ignorée", InfoBarSeverity.Warning); return; }

            try
            {
                var newLabel = new ProductLabel { ProductName = ProductName, BarcodeContent = ScanInput, Source = "PC_Local", ScannedAt = DateTime.Now, Status = "Printed" };
                _context.Labels.Add(newLabel);
                await _context.SaveChangesAsync();
                HistoryList.Insert(0, newLabel);

                // TOUS les modes impriment localement ce qu'ils scannent
                await PrintCurrentLabel();

                ScanInput = "";
                _ = SyncData();
            }
            catch (Exception ex) { LoggerService.Current.Log("Err Scan", LogLevel.Error, ex); UpdateStatus("Erreur enregistrement", InfoBarSeverity.Error); }
        }

        [RelayCommand]
        public async Task PrintOnly() => await PrintCurrentLabel();

        // ... (ProcessPendingItem, PrintAllPending, DeleteLabel, LoadLabelForEditing, RefreshHistory inchangés) ...
        [RelayCommand]
        public async Task ProcessPendingItem(ProductLabel label)
        {
            if (label == null) return;
            try
            {
                ProductName = label.ProductName; ScanInput = label.BarcodeContent; await PrintCurrentLabel();
                var item = await _context.Labels.FindAsync(label.Id); if (item != null) { item.Status = "Printed"; await _context.SaveChangesAsync(); }
                LoadData();
            }
            catch (Exception ex) { LoggerService.Current.Log("Err Print Pending", LogLevel.Error, ex); }
        }

        [RelayCommand]
        public async Task PrintAllPending()
        {
            if (PendingList.Count == 0) return; UpdateStatus("Impression lot...", InfoBarSeverity.Informational);
            foreach (var i in PendingList.ToList()) { await ProcessPendingItem(i); await Task.Delay(1500); }
            UpdateStatus("Fini !", InfoBarSeverity.Success);
        }

        [RelayCommand] public async Task DeleteLabel(ProductLabel l) { if (l == null) return; var i = await _context.Labels.FindAsync(l.Id); if (i != null) { _context.Labels.Remove(i); await _context.SaveChangesAsync(); } LoadData(); }
        [RelayCommand] public void LoadLabelForEditing(ProductLabel l) { if (l == null) return; ProductName = l.ProductName; ScanInput = l.BarcodeContent; }
        [RelayCommand] public void RefreshHistory() { LoadData(); _ = SyncData(); }

        private void LoadData()
        {
            try
            {
                var q = _context.Labels.AsQueryable();
                if (!string.IsNullOrWhiteSpace(SearchText)) q = q.Where(x => x.ProductName.Contains(SearchText) || x.BarcodeContent.Contains(SearchText));
                var l = q.OrderByDescending(x => x.ScannedAt).Take(100).ToList();
                PendingList.Clear(); HistoryList.Clear();
                foreach (var i in l) { if (i.Status == "Pending") PendingList.Add(i); else HistoryList.Add(i); }
            }
            catch { }
        }

        [RelayCommand]
        public async Task SyncData()
        {
            if (_isSyncing) return;
            // SI MONOPOSTE -> PAS DE SYNC
            if (AppSettings.AppMode == "Standalone") return;

            _isSyncing = true;
            try
            {
                await Task.Run(async () => {
                    using var rDb = new RemoteDbContext();
                    if (!await rDb.Database.CanConnectAsync()) return;
                    await rDb.Database.EnsureCreatedAsync();

                    // 1. Envoi
                    var loc = await _context.Labels.ToListAsync();
                    bool chg = false;
                    foreach (var l in loc)
                    {
                        if (!await rDb.Labels.AnyAsync(r => r.BarcodeContent == l.BarcodeContent && r.ScannedAt == l.ScannedAt))
                        {
                            rDb.Labels.Add(new ProductLabel { ProductName = l.ProductName, BarcodeContent = l.BarcodeContent, ScannedAt = l.ScannedAt, Source = l.Source, Status = l.Status }); chg = true;
                        }
                    }
                    if (chg) await rDb.SaveChangesAsync();

                    // 2. Réception
                    var rem = await rDb.Labels.Where(x => x.Source != "PC_Local").ToListAsync();
                    int add = 0;
                    foreach (var r in rem)
                    {
                        if (!await _context.Labels.AnyAsync(l => l.BarcodeContent == r.BarcodeContent && l.ScannedAt == r.ScannedAt))
                        {
                            _context.Labels.Add(new ProductLabel { ProductName = r.ProductName, BarcodeContent = r.BarcodeContent, ScannedAt = r.ScannedAt, Source = r.Source, Status = (r.Status ?? "Pending") }); add++;
                        }
                    }
                    if (add > 0)
                    {
                        await _context.SaveChangesAsync();
                        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(LoadData);
                        UpdateStatus($"{add} étiquettes reçues", InfoBarSeverity.Success);
                    }
                });
            }
            catch { }
            finally { _isSyncing = false; }
        }

        public void UpdateStatus(string msg, InfoBarSeverity sev = InfoBarSeverity.Informational)
        {
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() => {
                InfoBarMessage = msg; InfoBarSeverity = sev; IsInfoBarOpen = true;
                if (sev == InfoBarSeverity.Success) { var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) }; t.Tick += (s, e) => { IsInfoBarOpen = false; t.Stop(); }; t.Start(); }
            });
        }
        private async Task PrintCurrentLabel() { if (QrCodeImage != null) PrintRequested?.Invoke(this, EventArgs.Empty); await Task.CompletedTask; }
        private void GenerateQrPreview(string c) { if (string.IsNullOrWhiteSpace(c)) { QrCodeImage = null; return; } try { using var g = new QRCodeGenerator(); using var d = g.CreateQrCode(c, QRCodeGenerator.ECCLevel.Q); using var p = new PngByteQRCode(d); var b = p.GetGraphic(20); using var s = new InMemoryRandomAccessStream(); using (var w = new DataWriter(s.GetOutputStreamAt(0))) { w.WriteBytes(b); w.StoreAsync().AsTask().Wait(); } s.Seek(0); var i = new BitmapImage(); i.SetSource(s); QrCodeImage = i; } catch { } }
    }
}