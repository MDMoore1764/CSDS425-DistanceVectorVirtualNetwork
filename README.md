## Distance Vector Virtual Network Routing

### Running the Project

The project is a C# project. The project must first be compiled using

```bash
dotnet publish
```

This must be run at the root of the project or within the Simulator directory.
This will create a `publish` directory with the compiled project: `Simulator/bin/Debug/net9.0/publish/Simulator.exe`.

Execute this executable to run the project.

`Note:` in Linux, this will not include the .exe extension.

### Output

The project produces a network distance map, containing an entry for each client and their respective distances to all other clients. This is located in the `Output/Distances.json` file.

### Components

#### Server

The server is represented by the Server class. There is only ever one server.

The server is responsible for maintaining connections with clients,
forwarding update messages to the sending client's neighbors upon receipt,
and maintaining and logging the most up-to-date routing table.

The server is initialized with a list of connected clients and their respective distances to their neighbors.
The server only sends initial distances to its clients once all clients have connected.

#### Client

Each client is represented by the Client class. There are many clients active simultaneously.
Clients connect to the server with the Join message, which includes their client identity.

If a client receives an update message, it will attempt to update its distances
given the distances provided by the update message by following the distance vector equation.
If the client's distances change, it will send an update message to the server with its new distances,
which will then be forwarded to the client's neighbors.

#### Simulator

The simulator simulates the process as described by the project description:

1. The server is initialized on a separate lightweight thread and it's address and port are recorded.
2. Each client identity specified in the project definition is connected to the server one-by-one until all clients have connected.
3. The server sends the initial distances to each client.
4. The clients, upon receiving their initial distances, will send update messages to the server with their distances.
5. The server will forward each update message to the respective clients' neighbors.
6. The clients will update their distances and send new update messages to the server if their distances change.
7. This process will continue until all client distances have stabilized.
