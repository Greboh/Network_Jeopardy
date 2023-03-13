using System.Net.Sockets;
using System.Security.Principal;
using SharedLogic;
using SharedLogic.Package;
using SharedLogic.TcpHelper;
using TcpServer;
using WebApplication1.Controllers;

namespace Tcp.Server;

/// <summary>
/// Handles everything about the Jeopardy GameServer
/// </summary>
public class JeopardyGameServer : IGameServer
{
    #region Fields

    /// <summary>
    /// Reference to the main server
    /// </summary>
    private Server _server;

    public string GameName => "Jeopardy";
    public PlayerCount Count => new() {minPlayers = 1, maxPlayers = 6};

    public Dictionary<Guid, ClientData> ConnectedClients { get; } = new();
    public Guid ID { get; } = Guid.NewGuid();
    public GameStatus Status { get; set; }
    public bool HasHadPlayers { get; set; }
    public GameStates GameState { get; set; }
    
    public List<QuestionsModel> QList { get; set; }
    public List<CategoriesModel> CList { get; set; }
    
    public List<QuestionsModel> CQList { get; set; }
    public string CategoryChosen { get; set; } = string.Empty;
    public int QuestionChosen { get; set; } = -1;
    //Stores the maximum attempts users can collectivle use before it removes question
    public int MaxAttempts = 12;
    public int AnswerAttempts { get; set; } = 0;

    private string? _weclome;
    private string? _join;
    private string? _amount;
    private string? _owner;
    

    #endregion

    public JeopardyGameServer(Server server, List<QuestionsModel> qList, List<CategoriesModel> cList)
    {
        _server = server;
        QList = qList;
        CList = cList;
    }
    
    public void StartGame()
    {
        // Set the status to started
        Status = GameStatus.Started;
        
        // While the game is started
        while (Status == GameStatus.Started)
        {
            // Spare cpu resources and only call it with 1 second delay
            Thread.Sleep(1000);
            EmptyGameCheck();
        }

        UpdateGame();
    }



    public void UpdateGame()
    {
        GameState = GameStates.PickCategory;
        while (Status == GameStatus.Running)
        {
         
            EmptyGameCheck();

            Console.WriteLine(GameName);
            
            switch (GameState)
            {
                case GameStates.PickCategory:
                    //default value is set to -1 for negating all questions becoming empty from the start
                    if (QuestionChosen != -1)
                    {
                        //Changes category and question to empty
                        //so that unpon filtering we can gather how many questions is left
                        QList[QuestionChosen].Question = string.Empty;
                        QList[QuestionChosen].Category = string.Empty;
                    }
                    
                    //if the category chosen is empty we dont add it to the category list
                    if (CategoryChosen != string.Empty)
                    {
                        List<QuestionsModel> tmpQlist = QList.FindAll(q => q.Category == CategoryChosen);
                        if (tmpQlist.Count <= 0)
                        {
                            CList.Find(c => c.Category.ToLower() == CategoryChosen.ToLower()).empty = true;
                        }
                    }
                    // sets values to default at gameserver start
                    CategoryChosen = "";
                    QuestionChosen = -1;
                    //sends package of categoriesList to all client within the game lobby
                    _server.SendPackageToClients(PackageType.Categories, null,CList);
                    break;
                case GameStates.PickQuestion:
                    //Sends chosen category
                    _server.SendPackageToClients(PackageType.Questions, null,CategoryChosen);
                    break;
                case GameStates.AnswerQuestion:
                    //sends the chosen question within chosen category
                    _server.SendPackageToClients(PackageType.QuestionsQuestion, null,QuestionChosen);
                    break;
               
            }
            //after each change of gamestate it goes into loop awaiting packages from client to get the game moving on
            WaitResponse();

        }
    }

    /// <summary>
    /// Wait loop for after it await clients game choices for
    /// the differnt gameStates
    /// </summary>
    private void WaitResponse()
    {
        GameStates tmpState = GameState;
        
        while (GameState == tmpState && Status != GameStatus.Stopped)
        {
            if (AnswerAttempts >= MaxAttempts)
            {
                GameState = GameStates.PickCategory;
                AnswerAttempts = 0;
            }

            EmptyGameCheck();
        }
    }

    public void Close()
    {
        // Removes the GameServer from our Servers list of all GameServers
        _server.GameServers.Remove(this);
    }

    public void AddPlayer(ClientData client)
    {
        // If the client is the first to connect, make them GameOwner
        if (ConnectedClients.Count == 0) client.IsGameOwner = true;
        
        HasHadPlayers = true;

        ConnectedClients.Add(client.NetworkID, client);
        
        // Create some strings that the client gets when he connected
        
        _weclome ??= $"Welcome to {GameName}!\nFirst a quick how-to guide to the {GameName} interface!" +
                     $"\nYou will receive a message from the game with categories available to pick a category use the command 'Choice' see help for more!";
        _owner ??= "You are Game Owner. At any point you wish to start the game, type Start";
        
        _join ??= $"{client.Username} joined the game!";
        _amount ??= $"Current players in this game {ConnectedClients.Count}/{Count.maxPlayers}";

        // Join the strings together to minimize how many messages gets sent 
        var welcomeMessage = $"\n{_weclome}\n{_owner}";
        var joinMessage = $"\n{_join}\n{_amount}\n";
        
        // Write the messages to the client(s)
        _server.SendPackageToClients(PackageType.GameMessage, client, welcomeMessage);
        _server.SendPackageToClients(PackageType.GameMessage, null, joinMessage);
    }

    public bool IsFull() => ConnectedClients.Count == Count.maxPlayers;

    public void DisconnectPlayer(ClientData client)
    {
        // Set the clients Status to disconnected
        client.Status = ClientConnectionStatus.Disconnected;
        
        // Remove him from the dictionary of connected clients
        ConnectedClients.Remove(client.NetworkID);
    }
    
    public void EmptyGameCheck()
    {
        // Make sure there is no clients connected or if it has had any players yet
        if (ConnectedClients.Count != 0 || !HasHadPlayers) return;
        
        // Stops the GameServer
        Status = GameStatus.Stopped;
        
        Console.WriteLine($"Closed GameServer {ID}");
        Close();
    }
}