using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Simulator.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Simulator.Server
{
    internal class Server
    {
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

        private Socket serverSocket;
        public Server(int port = 0)
        {
            this.connectedClients = [];


            var jsonString = File.ReadAllText(CONFIGURATION_FILE);
            this.initialDistances = JsonConvert.DeserializeObject<Dictionary<char, Dictionary<char, int>>>(jsonString)!;


            this.serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            this.serverSocket.Listen();
        }


        public static readonly int WINDOW_SIZE = short.MaxValue;
        public Task AcceptClientsAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var client = await this.serverSocket.AcceptAsync();


                    if (client == null)
                    {
                        continue;
                    }

                    _ = HandleClientAsync(client);
                }
            });
        }

        private Task HandleClientAsync(Socket client)
        {
            return Task.Run(async () =>
            {
                byte[] buffer = new byte[WINDOW_SIZE];

                while (client.Connected)
                {
                    int nReceived = await client.ReceiveAsync(buffer);
                    var decoded = Encoding.UTF8.GetString(buffer, 0, nReceived);

                    JObject message = JObject.Parse(decoded);

                    if (message == null)
                    {
                        continue;
                    }

                    var messageTypeValue = message.GetValue("Type");
                    if (messageTypeValue == null)
                    {
                        //TODO: Logic for incorrect message received.
                        continue;
                    }

                    var messageType = (MessageType)messageTypeValue.Value<int>();

                    switch (messageType)
                    {
                        case MessageType.JOIN:
                            var joinMessage = message.ToObject<JoinMessage>();
                            _ = HandleMessageReceivedAsync(client, joinMessage);
                            break;
                        case MessageType.UPDATE:
                            var updateMessage = message.ToObject<UpdateMessage>();
                            _ = HandleMessageReceivedAsync(client, updateMessage);
                            break;
                    }
                }
            });
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
                var updateMessage = new UpdateMessage(this.initialDistances[client.Key]);
                var encoded = updateMessage.Encode();

                await client.Value.SendAsync(encoded);
            }
        }

        private async Task HandleMessageReceivedAsync(Socket sender, UpdateMessage message)
        {
            Console.WriteLine("TODO: Update Message");
        }

        private bool AllClientsConnected()
        {
            return this.connectedClients.Count == AVAILABLE_IDENTITIES.Count;
        }
    }
}
