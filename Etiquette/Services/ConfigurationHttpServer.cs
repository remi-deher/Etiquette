using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Timers;

namespace Etiquette.Services
{
    public class ConfigurationHttpServer
    {
        private HttpListener _listener;
        private const int Port = 54322;

        // Timer pour le mode appairage
        private Timer _pairingTimer;
        private bool _isPairingActive = false;

        // Événement pour mettre à jour l'UI quand le temps est écoulé
        public event EventHandler<bool> PairingStatusChanged;

        public bool IsPairingActive => _isPairingActive;
        public bool IsListening => _listener != null && _listener.IsListening;

        public void Start()
        {
            if (_listener != null && _listener.IsListening) return;
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://*:{Port}/");
                _listener.Start();
                Task.Run(HandleIncomingConnections);
            }
            catch
            {
                try
                {
                    // Fallback localhost
                    _listener = new HttpListener();
                    _listener.Prefixes.Add($"http://localhost:{Port}/");
                    _listener.Start();
                    Task.Run(HandleIncomingConnections);
                }
                catch { }
            }
        }

        public void Stop()
        {
            StopPairing();
            _listener?.Stop();
            _listener = null;
        }

        // --- GESTION DU MODE APPAIRAGE ---

        public void StartPairing(int minutes)
        {
            StopPairing();

            _isPairingActive = true;
            PairingStatusChanged?.Invoke(this, true);

            _pairingTimer = new Timer(minutes * 60 * 1000);
            _pairingTimer.Elapsed += (s, e) => StopPairing();
            _pairingTimer.AutoReset = false;
            _pairingTimer.Start();
        }

        public void StopPairing()
        {
            if (_pairingTimer != null)
            {
                _pairingTimer.Stop();
                _pairingTimer.Dispose();
                _pairingTimer = null;
            }

            if (_isPairingActive)
            {
                _isPairingActive = false;
                PairingStatusChanged?.Invoke(this, false);
            }
        }

        // --- PROTOCOLE ---

        private async Task HandleIncomingConnections()
        {
            while (_listener != null && _listener.IsListening)
            {
                try
                {
                    var ctx = await _listener.GetContextAsync();

                    // SÉCURITÉ : Si mode appairage inactif, on rejette tout (403).
                    if (!_isPairingActive)
                    {
                        ctx.Response.StatusCode = 403;
                        ctx.Response.Close();
                        continue;
                    }

                    if (ctx.Request.Url.AbsolutePath == "/pair" && ctx.Request.HttpMethod == "POST")
                    {
                        await HandleSecureExchange(ctx);
                        // Optionnel : StopPairing(); // Fermer après un succès unique ?
                    }
                    else
                    {
                        ctx.Response.StatusCode = 404;
                        ctx.Response.Close();
                    }
                }
                catch { }
            }
        }

        private async Task HandleSecureExchange(HttpListenerContext ctx)
        {
            try
            {
                // 1. Lire la clé publique du client
                byte[] clientPublicKey;
                using (var ms = new MemoryStream())
                {
                    await ctx.Request.InputStream.CopyToAsync(ms);
                    clientPublicKey = ms.ToArray();
                }

                // 2. Initialiser Crypto et générer le secret
                using (var crypto = new CryptoService())
                {
                    crypto.DeriveSharedSecret(clientPublicKey);

                    // 3. Exporter la config (Sans PIN)
                    var configService = new ConfigurationService();
                    string jsonConfig = configService.ExportConfigurationToJson(null);

                    // 4. Chiffrer
                    byte[] encryptedConfig = crypto.EncryptData(jsonConfig);
                    byte[] serverPublicKey = crypto.GetPublicKey();

                    // 5. Répondre : [LenKey(4)] [ServerKey] [EncryptedData]
                    ctx.Response.StatusCode = 200;
                    using (var ms = new MemoryStream())
                    using (var writer = new BinaryWriter(ms))
                    {
                        writer.Write(serverPublicKey.Length);
                        writer.Write(serverPublicKey);
                        writer.Write(encryptedConfig);

                        byte[] responseBytes = ms.ToArray();
                        await ctx.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                }
                ctx.Response.Close();
            }
            catch
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
            }
        }
    }
}