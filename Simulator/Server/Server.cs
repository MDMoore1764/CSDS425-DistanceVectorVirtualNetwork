﻿using Newtonsoft.Json;
using Simulator.Messaging;
using System.Net;
using System.Net.Sockets;

namespace Simulator.Server
{
    internal class Server
    {
        public const int DEFAULT_DISTANCE = -1;
        public static readonly HashSet<char> AVAILABLE_IDENTITIES = new HashSet<char>
        {
            'u',
            'x',
            'w',
            'v',
            'y',
            'z'
        };

        private const string CONFIGURATION_FILE = "Server/Configuration/InitialDistances.json";
        private const string OUTPUT_FILE = "Output/Distances.json";
        private Dictionary<char, Socket> connectedClients;
        private Dictionary<char, Dictionary<char, int>> initialDistances;
        private Dictionary<char, Dictionary<char, int>> finalDistances;

        private Socket serverSocket;
        public Server(int port = 0)
        {
            this.connectedClients = [];

            //Read the initial distances from the configuration file.
            var jsonString = File.ReadAllText(CONFIGURATION_FILE);
            this.initialDistances = JsonConvert.DeserializeObject<Dictionary<char, Dictionary<char, int>>>(jsonString)!;


            //Initialize the final distance table with the initial distances.
            this.finalDistances = [];
            foreach (var (key, value) in this.initialDistances)
            {
                this.finalDistances.Add(key, value);
            }

            //Create the server TCP socket and bind it to the specified port.
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            IPAddress serverIP = localIPs.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);

            this.serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.serverSocket.Bind(new IPEndPoint(serverIP, port));
            this.serverSocket.Listen();

            var endpoint = (IPEndPoint)this.serverSocket.LocalEndPoint!;
            Console.WriteLine($"Server started at {endpoint.Address}:{endpoint.Port}");
        }


        public IPAddress? GetAddress()
        {
            if (this.serverSocket == null)
            {
                return null;
            }

            return ((IPEndPoint)this.serverSocket.LocalEndPoint!).Address;
        }

        public int? GetPort()
        {
            if (this.serverSocket == null)
            {
                return null;
            }

            return ((IPEndPoint)this.serverSocket.LocalEndPoint!).Port;
        }

        private object outputFileLock = new object();
        /// <summary>
        /// Prints the current routing table to the output file.
        /// </summary>
        private void WriteDistanceTable()
        {
            try
            {
                Monitor.Enter(outputFileLock);
                Directory.CreateDirectory(Path.GetDirectoryName(OUTPUT_FILE)!);
                var table = JsonConvert.SerializeObject(this.finalDistances, Formatting.Indented);
                File.WriteAllText(OUTPUT_FILE, table);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Monitor.Exit(outputFileLock);
            }
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

        /// <summary>
        /// Start the client receive loop, receiving and handling messages from the client.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Handle a join message received by a client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task HandleMessageReceivedAsync(Socket sender, JoinMessage message)
        {
            this.connectedClients[message.Identity] = sender;

            var clientEndpoint = (IPEndPoint)sender.RemoteEndPoint!;
            Console.WriteLine($"Client {message.Identity} has joined the server at {clientEndpoint.Address}:{clientEndpoint.Port}");

            if (!this.AllClientsConnected())
            {
                return;
            }


            Console.WriteLine("All clients have connected!");

            //Send update message to all clients with their initial distances.
            foreach (var client in this.connectedClients)
            {
                var updateMessage = new UpdateMessage(client.Key, this.initialDistances[client.Key]);
                var encoded = updateMessage.Encode();

                await client.Value.SendAsync(encoded);
            }
        }

        /// <summary>
        /// Handles an update message from a client, updating the routing table tracker (for report) and sending the updated distance vector to connected clients.
        /// </summary>
        /// <param name="sender">The client socket that sent the message</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        private async Task HandleMessageReceivedAsync(Socket sender, UpdateMessage message)
        {
            Console.WriteLine($"Received updated routing table from {message.Identity}.");

            //Update the final distance routing table tracker.
            this.finalDistances[message.Identity] = message.DistanceVector;

            //Encode the update message and forward it to all clients connected to the sender.
            var encoded = message.Encode();
            var connectedClientIdentities = this.initialDistances[message.Identity].Where(p => p.Key != message.Identity && p.Value != DEFAULT_DISTANCE).Select(p => p.Key).ToHashSet();
            var connectedClients = this.connectedClients.Where(p => connectedClientIdentities.Contains(p.Key)).Select(p => p.Value);

            Console.WriteLine($"{message.Identity}: Sending updated routing table to connected clients: {string.Join(",", connectedClientIdentities)}.");
            foreach (var connectedClient in connectedClients)
            {
                await connectedClient.SendAsync(encoded);
            }

            WriteDistanceTable();
        }

        /// <summary>
        /// Reports whether or not all clients have connected to the server.
        /// </summary>
        /// <returns></returns>
        private bool AllClientsConnected()
        {
            return this.connectedClients.Count == AVAILABLE_IDENTITIES.Count;
        }
    }
}
