using Simulator.Client;
using Simulator.Server;

Server server = new Server();
int serverPort = server.GetPort() ?? throw new Exception("Server failed to start.");

_ = server.AcceptClientsAsync();

List<Client> connectedClients = [];
foreach(var clientIdentity in Server.AVAILABLE_IDENTITIES)
{
    Client connectedClient = new Client(clientIdentity);

    await connectedClient.JoinAsync("localhost", serverPort);
    _ = connectedClient.ListenForUpdatesAsync();


    connectedClients.Add(connectedClient);

    await Task.Delay(150);
}

//test client:
await Task.Delay(-1);
