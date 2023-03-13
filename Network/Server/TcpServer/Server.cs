using System.Net;
using System.Net.Sockets;
using System.Text;
using AES_CBC;
using REST;
using SharedLogic;
using SharedLogic.Package;
using SharedLogic.Processor;
using SharedLogic.TcpHelper;
using TcpServer;
using WebApplication1.Controllers;

namespace Tcp.Server;

/// <summary>
/// Handles everything about the server
/// </summary>
public class Server
{
    #region Fields

    /// <summary>
    /// Contains all the GameServers
    /// </summary>
    public Dictionary<IGameServer, Thread> GameServers { get; set; } = new();

    /// <summary>
    /// Reference to our TcpListener which is our server
    /// </summary>
    TcpListener _server;

    /// <summary>
    /// Used to stop threads from continuing before they get a signal that they can
    /// </summary>
    ManualResetEvent _connectionLock = new(false);

    /// <summary>
    /// Controls if the server is running or not
    /// </summary>
    bool _running;

    /// <summary>
    /// Tracks all concurrent users / clients
    /// </summary>
    Dictionary<Guid, ClientData> _ccu = new();

    /// <summary>
    /// Tracks all users / clients in the lobby
    /// </summary>
    Dictionary<Guid, ClientData> _lobby = new();

    /// <summary>
    /// Tracks all users / clients in a GameServer
    /// </summary>
    Dictionary<Guid, ClientData> _inGame = new();


    HttpQueries _restAPi = new();
    List<QuestionsModel> _qList = new();
    List<CategoriesModel> _cList = new();
    private AesCrypto _aes = new AesCrypto();

    #endregion

    public Server()
    {
        // _restAPi.ListenUrl();
        // _restAPi.CreateQuestion(true);
        // Thread setQuizThread = new Thread(InitializeJeopardy);
        // setQuizThread.Start();

        // Make the server listen to any ip and only our defined port
        _server = new(IPAddress.Any, ConnectionHelper.port);
        _server.Start();

        // Set the server to running
        _running = true;

        // Update the server
        Update();
    }

    /// <summary>
    /// Updates the GameServer using a while loop
    /// </summary>
    private void Update()
    {
        try
        {
            // While the server is running loop 
            while (_running)
            {
                // Connects the clients
                ConnectClient();

                // Make the server wait with adding more clients until it has fully connected the current one
                _connectionLock.WaitOne();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    /// <summary>
    /// inserted into a thread that runs with the server to get the
    /// questions and categories from our API
    /// it is a thread and a loop for it has to have least
    /// standard generated questions from our api call question generator
    /// </summary>
    private async void InitializeJeopardy()
    {
        //Keeps going if base questions is gotten from server
        while (_qList.Count < 25)
        {
            _qList = await _restAPi.GetQuestions();
            _cList = await _restAPi.GetCategories();
        }
    }

    /// <summary>
    /// Create a new GameServer
    /// </summary>
    private void CreateGameServer()
    {
        // The server to create
        IGameServer server = new JeopardyGameServer(this, _qList, _cList);

        // The thread associated with this GameServer
        Thread serverThread = new(server.StartGame);

        // Add the GameServer to our Dictionary of GameServers
        GameServers.Add(server, serverThread);

        // Start the GameServer
        serverThread.Start();
    }

    #region Client Processing

    /// <summary>
    /// Connects the client
    /// </summary>
    private void ConnectClient()
    {
        try
        {
            // Sets the signal to locked
            _connectionLock.Reset();
            Console.WriteLine("Server Waiting for connection");
            TcpClient newClient = _server.AcceptTcpClient();
            Console.WriteLine($"Connection established to {newClient.Client.RemoteEndPoint}");

            // Create a new thread and start it .. Used to process the clients
            Thread clientConnectionThread = new(ProcessConnection) {IsBackground = true};
            clientConnectionThread.Start(newClient);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    /// <summary>
    /// Creates everything the server needs in order to communicate with the client
    /// </summary>
    /// <param name="tcpClient">The client that is connecting</param>
    private void ProcessConnection(object tcpClient)
    {
        try
        {
            // Retrieve client from parameter
            TcpClient client = (TcpClient) tcpClient;

            // Create the new client's data
            ClientData newClient = new ClientData
            {
                Tcp = client,
                ClientStream = client.GetStream(),
                IP = (IPEndPoint) client.Client.RemoteEndPoint,
                Reader = new(client.GetStream(), Encoding.UTF8),
                Writer = new(client.GetStream(), Encoding.UTF8)
            };

            // Make sure we get the AccountInfo for our tcpClient before continuing
            while (newClient.Status != ClientConnectionStatus.Connected)
            {
                // Read the incoming data 
                PackageReader(newClient);
            }

            Console.WriteLine($"New client added!\n{newClient}");

            // Add the client to concurrent users
            _ccu.Add(newClient.NetworkID, newClient);

            // Add client to server lobby
            _lobby.Add(newClient.NetworkID, newClient);

            // Create and start a thread for our new client
            Thread clientThread = new(ClientUpdate)
            {
                IsBackground = true
            };
            clientThread.Start(newClient);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
        // Tell the server to allow new clients to connect
        finally
        {
            _connectionLock.Set();
        }
    }

    /// <summary>
    /// Updates each individual client
    /// </summary>
    /// <param name="clientData">The clients data</param>
    private void ClientUpdate(object clientData)
    {
        try
        {
            ClientData client = (ClientData) clientData;

            while (client.Status == ClientConnectionStatus.Connected)
            {
                // Check if the client is disconnected by: 
                // Calling the client socket's poll method to read if the client is disconnected and also checking that no data is available from the client's stream
                if (client.Tcp.Client.Poll(1000, SelectMode.SelectRead) && !client.ClientStream.DataAvailable)
                    client.Status = ClientConnectionStatus.Disconnected;

                // Make sure again that there is anything to read!
                if (!client.ClientStream.DataAvailable) continue;
                PackageReader(client);
            }

            DisconnectClient(client);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// Adds an individual client to a GameServer
    /// </summary>
    /// <param name="client"></param>
    private void AddClientToGame(ClientData client)
    {
        // Check if there are any game servers if not create one
        if (GameServers.Count == 0)
        {
            CreateGameServer();
        }

        // Check if all servers are full or is running
        int fullServers = GameServers.Count(gameServer => gameServer.Key.IsFull() || gameServer.Key.Status == GameStatus.Running);

        // If they are create a new one
        if (fullServers == GameServers.Count) CreateGameServer();

        // Now there must be a gameserver that isn't full or running! so find that and add our player
        foreach (var gameServer in GameServers.Where(gameServer => !gameServer.Key.IsFull() || gameServer.Key.Status == GameStatus.Running))
        {
            _lobby.Remove(client.NetworkID);
            _inGame.Add(client.NetworkID, client);
            gameServer.Key.AddPlayer(client);
            Console.WriteLine($"Added {client.Username} to GameServer with ID: {gameServer.Key.ID} ");
            return;
        }
    }

    /// <summary>
    /// Disconnect a specific client
    /// </summary>
    /// <param name="client">The client to disconnect</param>
    private void DisconnectClient(ClientData client)
    {
        // Remove client from ccu
        _ccu.Remove(client.NetworkID);

        // Check which "Room" the player is in
        if (_lobby.ContainsKey(client.NetworkID)) _lobby.Remove(client.NetworkID);
        else if (_inGame.ContainsKey(client.NetworkID))
        {
            _inGame.Remove(client.NetworkID);

            foreach (var pair in GameServers.Where(pair => pair.Key.ConnectedClients.ContainsValue(client)))
            {
                pair.Key.DisconnectPlayer(client);
            }
        }

        var msg = $"{client.Username} has disconnected!";
        Console.WriteLine(msg);

        SendPackageToClients(PackageType.Broadcast, client, msg);
        client.Tcp.Close();
    }

    #endregion

    #region Read Package Methods

    /// <summary>
    /// Reads the packages from a client
    /// </summary>
    /// <param name="client">The package to read</param>
    private void PackageReader(ClientData client)
    {
        try
        {
            // Make sure that we aren't at the end of our stream
            if (client.Reader.EndOfStream) return;

            // GetPackage from stream
            string? incomingData = client.Reader.ReadLine();
            string decryptedData = _aes.Decrypter(incomingData);
            NetworkPackage package = PackageProcessor.ReadPackage(decryptedData);

            // Console.WriteLine($"\nReceived {package}");

            // A lot of the cases under in the switch does the same
            // A few will be commented but others wont since the they are almost identical

            // Switch on the package type
            switch (package.type)
            {
                case PackageType.AccountInfo:
                {
                    // Get the accountInfo from the packageData
                    AccountInfo message = (package.data as AccountInfoPackage).accountInfo;

                    // Call the method in ClientData to set the remaining data
                    client.SetRemainingData(message.username, message.password);
                }
                    break;

                case PackageType.Broadcast:
                {
                    // Get the message from the PackageData
                    var message = (package.data as MessagePackage)?.Message;

                    // Switch on the message
                    switch (message)
                    {
                        // if the message is "Start" and the client owns the GameServer
                        case "Start" when client.IsGameOwner:
                        {
                            // Set reference to the KeyValuePair we get from the method beneath 
                            KeyValuePair<IGameServer, Thread> server;

                            // if we find the client
                            if (FindClientInGameServer(out server))
                            {
                                // set the server status to running
                                server.Key.Status = GameStatus.Running;
                                Console.WriteLine($"Game {server.Key.ID} has started!");
                            }
                        }
                            break;

                        case "Join" when _lobby.ContainsKey(client.NetworkID):
                        {
                            AddClientToGame(client);
                        }
                            return;

                        // If the message doesn't contain "Start" or "Join" it must be meant as an Echo message
                        default:
                        {
                            Console.WriteLine($"{client.Username} > {message}");
                            SendPackageToClients(PackageType.Echo, client, message);
                        }
                            break;
                    }
                }
                    break;

                case PackageType.GameChoice:
                {
                    HandleGameChoice(client, package);
                }
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    private void HandleGameChoice(ClientData client, NetworkPackage networkPackage)
    {
        string message = (networkPackage.data as GameChoicePackage).Message;

        KeyValuePair<IGameServer, Thread> server;
        if (!FindClientInGameServer(out server)) return;

        switch (server.Key.GameState)
        {
            //When gameserver enters PickCategory State
            case GameStates.PickCategory:
            {
                foreach (CategoriesModel c in server.Key.CList)
                {
                    
                    //if a match from players message string is same as a category that is not empty = true
                    if (message.ToLower() == c.Category.ToLower() && c.empty == false)
                    {
                        //retunrs the message of chosen category
                        SendGameMessage(client, $"You picked {c.Category}");
                        //Stores the chosen category in GameServer
                        server.Key.CategoryChosen = c.Category;
                        //step out of our await loop in GameServer
                        server.Key.GameState = GameStates.PickQuestion;
                        //Updates CategoryQuestionList on GameServer
                        server.Key.CQList = server.Key.QList.FindAll(q => q.Category == c.Category);
                        return;
                    }
                }

                SendGameMessage(client, "Category does not exist..");
            }
                break;

            case GameStates.PickQuestion:
            {
                        int qId;
                        //tryes to turns player message into int 
                        //if not it returns question does not exist
                        Int32.TryParse(message, out qId);
                        if (qId != null)
                        { 
                            foreach (QuestionsModel q in server.Key.QList) 
                            { 
                                if (qId == q.Id && server.Key.CategoryChosen == q.Category && server.Key.QList[qId - 1].Question != string.Empty) 
                                { 
                                    server.Key.QuestionChosen = q.Id - 1; 
                                    SendGameMessage(client, $"You picked {qId}");
                                    server.Key.GameState = GameStates.AnswerQuestion;
                                    return; 
                                } 
                            }
                        }
                        SendGameMessage(client, "Question does not exist..");
                    } break;
                    

           

            case GameStates.AnswerQuestion:
                        //adds to answer attemps
                        server.Key.AnswerAttempts++;
                        //checkcs if the answer matches question answer
                        if (!_qList[server.Key.QuestionChosen].Answer.ToLower().Equals(message.ToLower()))
                        {
                            SendGameMessage(client, "Answer was incorrect... \n   Try Again..");
                        }
                        else
                        {
                            server.Key.GameState = GameStates.PickCategory;
                            SendGameMessage(client, "Answer was Correct");
                        } break;
        }
    }

    #endregion

    #region Send Package Methods

    /// <summary>
    /// Send a package to a client
    /// </summary>
    /// <param name="type">The type of the package</param>
    /// <param name="client">The client the package is meant for</param>
    /// <param name="message">The message of the package, depends on the type of the package</param>
    public void SendPackageToClients(PackageType type, ClientData client, object? message)
    {
        // A lot of the cases under in the switch does the same
        // A few will be commented but others wont since the they are almost identical
        
        switch (type)
        {
            // If the type is Broadcast
            case PackageType.Broadcast:
            {
                // Loop through all ccu's
                foreach (KeyValuePair<Guid, ClientData> data in _ccu)
                {
                    // send the message each client
                    SendMessage(data.Value, message.ToString());
                }
            }
                break;

            case PackageType.Echo:
            {
                // Loop through all ccu's where the senders ip is not the ccu's ip
                foreach (var data in _ccu.Where(c => c.Value.IP != client.IP))
                {
                    SendEchoPackage(client, data.Value, message.ToString());
                }
            }
                break;

            case PackageType.GameMessage:
            {
                FindClientFromGame(client, message.ToString());
            }
                break;

            case PackageType.Categories:
            {
                SendCategoriesMessage(client, (List<CategoriesModel>) message);
            }
                break;

            case PackageType.Questions:
            {
                SendCategoriesQuestions(client, message.ToString());
            }
                break;

            case PackageType.QuestionsQuestion:
            {
                int tmpChoice;
                int.TryParse(message.ToString(), out tmpChoice);
                SendsQuestionChosenByReward(client, tmpChoice);
            }
                break;
        }
    }

    /// <summary>
    /// Finds the client in a specific game
    /// </summary>
    /// <param name="client">The client to find</param>
    /// <param name="message">The message to send to that client</param>
    private void FindClientFromGame(ClientData client, string message)
    {
        foreach (KeyValuePair<Guid, ClientData> ccu in _ccu)
        {
            // First check if the ccu is in the lobby if so, return
            if (_lobby.ContainsKey(ccu.Key)) continue;

            // Then search all gameServers for the ccu's networkid
            foreach (KeyValuePair<IGameServer, Thread> server in GameServers)
            {
                if (!server.Key.ConnectedClients.ContainsKey(ccu.Key)) continue;
                // Now we can send to the correct clients

                // If client is null its to all players in the server
                if (client == null) SendGameMessage(ccu.Value, message);

                // if client is not null make sure that our ccu is our client so we can send a message to only him
                if (ccu.Value != client) continue;

                // if the message contains Game owner make sure that the ccu is the game owner
                if (message.Contains("Game Owner") && ccu.Value.IsGameOwner)
                {
                    SendGameMessage(ccu.Value, message);
                }
                // else it's a specific message to the ccu from the gameServer
                else SendGameMessage(ccu.Value, message);
            }
        }
    }
    
    // A lot of these send methods does the same
    // A few will be commented but others wont since the they are almost identical
    
    /// <summary>
    /// Sends an echo Package to a client
    /// </summary>
    /// <param name="fromClient">The client the message is from</param>
    /// <param name="toClient">The client the message is meant to for</param>
    /// <param name="echo">The message to the client</param>
    private void SendEchoPackage(ClientData fromClient, ClientData toClient, string echo)
    {
        // Create & Get the string from the EchoPacakge
        string messagePackage = PackageProcessor.CreateEchoPackage($"{fromClient.Username} > {echo}");
        
        // Encrypt the string
        string encryptedMessage = _aes.Encrypter(messagePackage);
        if (messagePackage.Length <= 0) return;

        ConnectionHelper.WriteToStream(toClient.Writer, encryptedMessage);
    }

    private void SendMessage(ClientData toClient, string message)
    {
        string messagePackage = PackageProcessor.CreateBroadcastMessage($"Server > {message}");
        string encryptedMessage = _aes.Encrypter(messagePackage);
        if (messagePackage.Length <= 0) return;

        ConnectionHelper.WriteToStream(toClient.Writer, encryptedMessage);
    }

    private void SendGameMessage(ClientData toClient, string message)
    {
        string messagePackage = PackageProcessor.CreateGameMessagePackage($"Game > {message}");
        string encryptedMessage = _aes.Encrypter(messagePackage);
        if (encryptedMessage.Length <= 0) return;

        ConnectionHelper.WriteToStream(toClient.Writer, encryptedMessage);
    }

    private void SendCategoriesMessage(ClientData toClient, List<CategoriesModel> cList)
    {
        string message = PackageProcessor.CreateCategoriesPackage(cList);
        string encryptedMessage = _aes.Encrypter(message);
        if (message.Length <= 0) return;

        foreach (KeyValuePair<Guid, ClientData> ccu in _ccu)
        {
            // First check if the ccu is in the lobby if so, return
            if (_lobby.ContainsKey(ccu.Key)) continue;

            // Then search all gameServers for the ccu's networkid
            foreach (KeyValuePair<IGameServer, Thread> pair in GameServers)
            {
                if (!pair.Key.ConnectedClients.ContainsKey(ccu.Key)) continue;
                // Now we can send to the correct clients

                // If client is null its to all players in the server
                if (toClient == null) ConnectionHelper.WriteToStream(ccu.Value.Writer, encryptedMessage);
            }
        }
    }

    private void SendCategoriesQuestions(ClientData toClient, string category)
    {
        List<QuestionsModel> tmpList = _qList.Where(q => q.Category.Contains(category) && q.Question != string.Empty).ToList();

        //TODO Encrypt
        string message = PackageProcessor.CreateQuestionsPackage(tmpList);
        string encryptedMessage = _aes.Encrypter(message);
        if (message.Length <= 0) return;

        foreach (KeyValuePair<Guid, ClientData> ccu in _ccu)
        {
            // First check if the ccu is in the lobby if so, return
            if (_lobby.ContainsKey(ccu.Key)) continue;

            // Then search all gameServers for the ccu's networkid
            foreach (KeyValuePair<IGameServer, Thread> pair in GameServers)
            {
                if (!pair.Key.ConnectedClients.ContainsKey(ccu.Key)) continue;
                // Now we can send to the correct clients

                // If client is null its to all players in the server
                if (toClient == null) ConnectionHelper.WriteToStream(ccu.Value.Writer, encryptedMessage);
            }
        }
    }

    private void SendsQuestionChosenByReward(ClientData toClient, int choice)
    {
        // TODO Encrypt
        string message = PackageProcessor.CreateQuestionPackage(_qList[choice]);
        string encryptedMessage = _aes.Encrypter(message);
        if (message.Length <= 0) return;

        //Encrypts jsonString of serilizedObject
        //string encryptedMessagePackage = AesCrypto.Encrypter(messagePackage);

        foreach (KeyValuePair<Guid, ClientData> ccu in _ccu)
        {
            // First check if the ccu is in the lobby if so, return
            if (_lobby.ContainsKey(ccu.Key)) continue;

            // Then search all gameServers for the ccu's networkid
            foreach (KeyValuePair<IGameServer, Thread> pair in GameServers)
            {
                if (!pair.Key.ConnectedClients.ContainsKey(ccu.Key)) continue;
                // Now we can send to the correct clients

                // If client is null its to all players in the server
                if (toClient == null) ConnectionHelper.WriteToStream(ccu.Value.Writer, encryptedMessage);
            }
        }
    }

    #endregion

    /// <summary>
    ///  Finds a client from ccu who is in a specific GameServer
    /// </summary>
    /// <param name="server">The KeyValuePair reference that gets send out if we can find the client in a GameServer</param>
    /// <returns></returns>
    private bool FindClientInGameServer(out KeyValuePair<IGameServer, Thread> server)
    {
        // Loop through all GameServers
        foreach (var gameServer in GameServers)
        {
            // Loop through all clients that are inGame and find the GameServer that the client is in
            foreach (var ccu in _inGame.Where(c => gameServer.Key.ConnectedClients.ContainsValue(c.Value)))
            {
                // Set the out references
                server = gameServer;

                // Return true because we found the client
                return true;
            }
        }

        // Set the out reference
        server = default;

        // Return false because we didn't find the client
        return false;
    }
}
