using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Etiquette.Services
{
    public class NetworkDiscoveryService
    {
        private const int DiscoveryPort = 54321;
        private const string DiscoveryRequest = "ETIQUETTE_WHO_IS_THERE";
        private const string DiscoveryResponsePrefix = "ETIQUETTE_I_AM_HERE|";

        private CancellationTokenSource _cancellationTokenSource;
        private UdpClient _udpServer;

        // --- CÔTÉ SERVEUR (Celui qui partage sa config) ---

        public void StartListening()
        {
            StopListening(); // Sécurité pour ne pas en lancer deux

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => ListenLoop(_cancellationTokenSource.Token));
        }

        public void StopListening()
        {
            _cancellationTokenSource?.Cancel();
            _udpServer?.Close();
            _udpServer = null;
        }

        private async Task ListenLoop(CancellationToken token)
        {
            try
            {
                _udpServer = new UdpClient(DiscoveryPort);

                while (!token.IsCancellationRequested)
                {
                    // On attend un message
                    var result = await _udpServer.ReceiveAsync(token);
                    string message = Encoding.UTF8.GetString(result.Buffer);

                    if (message == DiscoveryRequest)
                    {
                        // Quelqu'un nous cherche ! On répond.
                        string myIp = GetLocalIpAddress();
                        if (!string.IsNullOrEmpty(myIp))
                        {
                            string response = $"{DiscoveryResponsePrefix}{myIp}";
                            byte[] responseBytes = Encoding.UTF8.GetBytes(response);

                            // On répond directement à celui qui a posé la question
                            await _udpServer.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Arrêt normal
            }
            catch (Exception ex)
            {
                // Erreur (ex: Port déjà utilisé)
                System.Diagnostics.Debug.WriteLine($"Erreur Discovery Service : {ex.Message}");
            }
        }

        // --- CÔTÉ CLIENT (Le nouveau PC) ---

        public async Task<string> SearchForServerAsync(int timeoutMs = 3000)
        {
            using (var udpClient = new UdpClient())
            {
                udpClient.EnableBroadcast = true;

                // On envoie le message à tout le monde (255.255.255.255)
                var endpoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
                byte[] bytes = Encoding.UTF8.GetBytes(DiscoveryRequest);

                await udpClient.SendAsync(bytes, bytes.Length, endpoint);

                // On attend une réponse avec un timeout
                var receiveTask = udpClient.ReceiveAsync();
                var timeoutTask = Task.Delay(timeoutMs);

                var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

                if (completedTask == receiveTask)
                {
                    // On a reçu une réponse !
                    var result = await receiveTask; // Récupère le résultat
                    string message = Encoding.UTF8.GetString(result.Buffer);

                    if (message.StartsWith(DiscoveryResponsePrefix))
                    {
                        // On extrait l'IP (Format: "PREFIX|192.168.1.25")
                        string serverIp = message.Split('|')[1];
                        return serverIp;
                    }
                }
            }

            return null; // Pas trouvé
        }

        // --- UTILITAIRE ---

        private string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                // On cherche une IPv4 qui n'est pas localhost (127.0.0.1)
                if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.ToString().StartsWith("127."))
                {
                    return ip.ToString();
                }
            }
            return null;
        }
    }
}