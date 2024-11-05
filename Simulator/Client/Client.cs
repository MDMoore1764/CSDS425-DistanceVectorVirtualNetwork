using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Simulator.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Simulator.Client
{
    internal class Client
    {
        //private static readonly HashSet<char> AVAILABLE_IDENTITIES = [
        //    'u',
        //    'x',
        //    'w',
        //    'v',
        //    'y',
        //    'z'
        //    ];


        private Dictionary<char, int> routerTable;
        private char identity;

        public Client(char identity)
        {
            if (!Server.Server.AVAILABLE_IDENTITIES.Contains(identity))
            {
                throw new ArgumentException("Invalid identity", nameof(identity));
            }

            this.identity = identity;
            this.routerTable = [];
        }

        private Socket clientSocket;
        public async Task JoinAsync(string serverAddress, int serverPort, int clientPort = 0)
        {
            var joinMessage = new JoinMessage(this.identity);

            this.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.clientSocket.Bind(new IPEndPoint(IPAddress.Any, clientPort));

            await this.clientSocket.ConnectAsync(serverAddress, serverPort);


            var encoded = joinMessage.Encode();

            await this.clientSocket.SendAsync(encoded);
        }

        public async Task ListenForUpdatesAsync()
        {
            if(this.clientSocket == null || !this.clientSocket.Connected)
            {
                throw new InvalidOperationException("The client is not connected!");
            }

            byte[] buffer = new byte[Server.Server.WINDOW_SIZE];


            while (this.clientSocket.Connected)
            {
                int nReceived = await clientSocket.ReceiveAsync(buffer);
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
                    case MessageType.UPDATE:
                        var updateMessage = message.ToObject<UpdateMessage>();
                        _ = HandleMessageReceivedAsync(updateMessage);
                        break;
                }
            }
        }

        private async Task HandleMessageReceivedAsync(UpdateMessage message)
        {
            Console.WriteLine($"Client {this.identity} - received update message");
        }
    }
}
