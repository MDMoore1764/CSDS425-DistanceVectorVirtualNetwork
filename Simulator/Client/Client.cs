using Simulator.Messaging;
using System.Net;
using System.Net.Sockets;

namespace Simulator.Client
{
    internal class Client
    {
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


            foreach (var destination in Server.Server.AVAILABLE_IDENTITIES)
            {
                if (destination == this.identity)
                {
                    continue;
                }

                this.routerTable[destination] = Server.Server.DEFAULT_DISTANCE;
            }
        }

        private const string LOCAL_HOST = "127.0.0.1";
        private Socket? clientSocket;
        
        /// <summary>
        /// Join the server at the specified address and port.
        /// </summary>
        /// <param name="serverAddress">The address of the server.</param>
        /// <param name="serverPort">The server port.</param>
        /// <param name="clientPort">The port of this client.</param>
        /// <returns></returns>
        public async Task JoinAsync(IPAddress serverAddress, int serverPort, int clientPort = 0)
        {
            var joinMessage = new JoinMessage(this.identity);

            this.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            this.clientSocket.Bind(new IPEndPoint(IPAddress.Any, clientPort));

            await this.clientSocket.ConnectAsync(serverAddress, serverPort);

            var encoded = joinMessage.Encode();

            await this.clientSocket.SendAsync(encoded);
        }

        /// <summary>
        /// Listens for updates from the server, if connected.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If the client has not joined the server.</exception>
        public async Task ListenForUpdatesAsync()
        {
            if (this.clientSocket == null || !this.clientSocket.Connected)
            {
                throw new InvalidOperationException("The client is not connected!");
            }

            try
            {

            byte[] buffer = new byte[Server.Server.WINDOW_SIZE];

            while (this.clientSocket.Connected)
            {
                //It's possible to have multiple pending messages. Separate these and handle each.
                int nReceived = await clientSocket.ReceiveAsync(buffer);
                var messages = Message.Decode(buffer[..nReceived]);


                foreach(var message in messages){
                    switch (message.Type)
                    {
                        case MessageType.UPDATE:
                            HandleMessageReceivedAsync((UpdateMessage)message);
                            break;
                    }
                }
            }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An error occurred while listening for updates: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Handles an update message received from the server, updating the client's routing table with the received distance vector.
        /// </summary>
        /// <param name="message"></param>
        private void HandleMessageReceivedAsync(UpdateMessage message)
        {
            Console.WriteLine($"Client {this.identity} - received update message from {message.Identity}.");

            var hasDistanceToSender = this.routerTable.TryGetValue(message.Identity, out var distanceToSender);

            //Only accept messages claiming to be from this client (initialization message) and messages from clients to which we have a distance.
            //If this code block is entered, something is implemented incorrectly at the server level.
            if (!hasDistanceToSender && message.Identity != this.identity)
            {
                throw new Exception("Received message from client to which no distance is recorded.");
            }


            bool routingTableUpdated = false;
            foreach (var (destination, currentDistance) in this.routerTable)
            {
                var hasNewDistance = message.DistanceVector.TryGetValue(destination, out var newDistance);

                //There's no distance known to that thing. Ignore this value.
                if (!hasNewDistance || newDistance == Server.Server.DEFAULT_DISTANCE)
                {
                    continue;
                }

                //The total distance to get to the destination is the distance from the sender to the destination plus the distance from this client to the sender.
                var newTotalDistance = newDistance + distanceToSender;

                //Update our routing table if this new distance is shorter than the currently known distance, or if the currently tracked distance is the default distance.
                if (IsLessThan(newTotalDistance, currentDistance))
                {
                    this.routerTable[destination] = newTotalDistance;

                    routingTableUpdated = true;
                }
            }

            if (routingTableUpdated)
            {
                var updateMessage = new UpdateMessage(this.identity, this.routerTable);
                var encoded = updateMessage.Encode();

                _ = this.clientSocket?.SendAsync(encoded);
            }
        }

        /// <summary>
        /// Measures whether thingThatMightBeLessThan is less than thingThatMightBeGreaterThan. Accounts for the default distance value.
        /// </summary>
        /// <param name="thingThatMightBeLessThan">The value we are checking to see if less than the second value.</param>
        /// <param name="thingThatMightBeGreaterThan">Teh value we are checking to see if is greater than the first value.</param>
        /// <returns></returns>
        private static bool IsLessThan(int thingThatMightBeLessThan, int thingThatMightBeGreaterThan)
        {
            if (thingThatMightBeLessThan == Server.Server.DEFAULT_DISTANCE)
            {
                return false;
            }

            if (thingThatMightBeGreaterThan == Server.Server.DEFAULT_DISTANCE)
            {
                return true;
            }

            //This is <, not <=, because a shorter number of hops is preferred.
            return thingThatMightBeLessThan < thingThatMightBeGreaterThan;
        }
    }
}
