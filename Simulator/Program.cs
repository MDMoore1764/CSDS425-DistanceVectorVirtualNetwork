using Simulator.Client;
using Simulator.Server;
using System.Net;

//Create the server and allow it to accept clients in the background.
Server server = new Server();
_ = server.AcceptClientsAsync();

IPAddress serverAddress = server.GetAddress() ?? throw new Exception("Server failed to start.");
int serverPort = server.GetPort() ?? throw new Exception("Server failed to start.");

//Create each client and connect each to the server. A delay is added for fun.
List<Client> connectedClients = [];
foreach(var clientIdentity in Server.AVAILABLE_IDENTITIES)
{
    Client connectedClient = new Client(clientIdentity);

    await connectedClient.JoinAsync(serverAddress, serverPort);
    _ = connectedClient.ListenForUpdatesAsync();

    connectedClients.Add(connectedClient);

    await Task.Delay(300);
}

//test client
await Task.Delay(-1);
