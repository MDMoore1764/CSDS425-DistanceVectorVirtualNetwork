

//create server and let it listen.

using Simulator.Client;
using Simulator.Server;

int serverPort = 8080;

Server server = new Server(serverPort);


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
