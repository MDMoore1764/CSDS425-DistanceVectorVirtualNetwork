using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Simulator.Messaging;
using Simulator.ThreadLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Simulator.Server
{
    internal class Server
    {
        public const int DEFAULT_DISTANCE = -1;

        private const string CONFIGURATION_FILE = "Server/Configuration/InitialDistances.json";
        public static readonly HashSet<char> AVAILABLE_IDENTITIES = new HashSet<char>
        {
            'u',
            'x',
            'w',
            'v',
            'y',
            'z'
        };

        private Dictionary<char, Socket> connectedClients;

        private Dictionary<char, Dictionary<char, int>> initialDistances;
        private Dictionary<char, Dictionary<char, int>> finalDistances;

        private Socket serverSocket;
        public Server(int port = 0)
        {
            this.connectedClients = [];


            var jsonString = File.ReadAllText(CONFIGURATION_FILE);
            this.initialDistances = JsonConvert.DeserializeObject<Dictionary<char, Dictionary<char, int>>>(jsonString)!;
            this.finalDistances = JsonConvert.DeserializeObject<Dictionary<char, Dictionary<char, int>>>(jsonString)!;


            this.serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            this.serverSocket.Listen();
        }


        public static readonly int WINDOW_SIZE = short.MaxValue;
        public Task AcceptClientsAsync(CancellationToken cancellationToken = default)
        {
            //Create accept loop in separate thread.
            return Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var client = await this.serverSocket.AcceptAsync();

                        if (client == null)
                        {
                            continue;
                        }

                        //Handle client in a separate thread.
                        _ = Task.Run(() => HandleClientAsync(client));
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"An error occurred while accepting clients: {ex.Message}");
                    throw;
                }
            }, cancellationToken);
        }

        private async Task HandleClientAsync(Socket client)
        {
            try
            {
                byte[] buffer = new byte[WINDOW_SIZE];

                while (client.Connected)
                {
                    int nReceived = await client.ReceiveAsync(buffer);
                    var messages = Message.Decode(buffer[..nReceived]);


                    foreach (var message in messages)
                    {
                        switch (message.Type)
                        {
                            case MessageType.JOIN:
                                await HandleMessageReceivedAsync(client, (JoinMessage)message);
                                break;
                            case MessageType.UPDATE:
                                await HandleMessageReceivedAsync(client, (UpdateMessage)message);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An error occurred while handling client: {ex.Message}");
                throw;
            }
        }

        private async Task HandleMessageReceivedAsync(Socket sender, JoinMessage message)
        {
            this.connectedClients[message.Identity] = sender;

            Console.WriteLine($"Client {message.Identity} has joined the server.");

            if (!this.AllClientsConnected())
            {
                return;
            }

            Console.WriteLine("All clients have connected!");

            //Send update message to all clients with their corresponding initial distances.
            foreach (var client in this.connectedClients)
            {
                var updateMessage = new UpdateMessage(client.Key, this.initialDistances[client.Key]);
                var encoded = updateMessage.Encode();

                await client.Value.SendAsync(encoded);
            }
        }

        private TimeSpan lastActivityPrintDelay = TimeSpan.FromSeconds(2);
        private System.Timers.Timer routingTablePrintoutTimer;
        private async Task HandleMessageReceivedAsync(Socket sender, UpdateMessage message)
        {
            Console.WriteLine($"Received updated routing table from {message.Identity}.");

            this.finalDistances[message.Identity] = message.DistanceVector;

            var encoded = message.Encode();
            var connectedClientIdentities = this.initialDistances[message.Identity].Where(p => p.Key != message.Identity && p.Value != DEFAULT_DISTANCE).Select(p => p.Key).ToHashSet();
            var connectedClients = this.connectedClients.Where(p => connectedClientIdentities.Contains(p.Key)).Select(p => p.Value);

            Console.WriteLine($"{message.Identity}: Sending updated routing table to connected clients: {string.Join(",", connectedClientIdentities)}.");
            foreach (var connectedClient in connectedClients)
            {
                await connectedClient.SendAsync(encoded);
            }


            if(routingTablePrintoutTimer != null)
            {
                routingTablePrintoutTimer.Enabled = false;
                routingTablePrintoutTimer.Close();
            }

            routingTablePrintoutTimer = new System.Timers.Timer();
            routingTablePrintoutTimer.Elapsed += (a, b) =>
            {
                Console.WriteLine("Routing Table:");
                foreach (var pair in this.finalDistances)
                {
                    Console.WriteLine($"Routing table for {pair.Key}:");
                    foreach (var fp in pair.Value)
                    {
                        Console.WriteLine($"\t{pair.Key} -> {fp.Key}: {fp.Value}");
                    }
                }

                routingTablePrintoutTimer.Enabled = false;
            };

            routingTablePrintoutTimer.Interval = lastActivityPrintDelay.TotalMilliseconds;
            routingTablePrintoutTimer.Enabled = true;
            routingTablePrintoutTimer.Start();
        }

        private bool AllClientsConnected()
        {
            return this.connectedClients.Count == AVAILABLE_IDENTITIES.Count;
        }
    }
}
