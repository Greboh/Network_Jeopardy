using System.Net.Sockets;
using SharedLogic;
using Tcp.Server;
using WebApplication1.Controllers;

namespace TcpServer;

/// <summary>
/// Struct used to define a minimum & maximum amount of players in the GameServer
/// </summary>
public struct PlayerCount
{
    public int minPlayers;
    public int maxPlayers;
}

/// <summary>
/// The Game Servers status 
/// </summary>
public enum GameStatus
{
    Started,
    Running,
    Stopped
}

/// <summary>
/// The Game states
/// </summary>
public enum GameStates
{
    PickCategory,
    PickQuestion,
    AnswerQuestion,
}

/// <summary>
/// Interface used to create a GameServer
/// Contains all information required for the server to create a GameServer and for the GameServer to operate
/// </summary>
public interface IGameServer
{
    /// <summary>
    /// The name of the game
    /// </summary>
    public string GameName { get; }
    
    /// <summary>
    /// The min & max count of players
    /// </summary>
    public PlayerCount Count { get; }
    
    /// <summary>
    /// Stores the clients that is connected to this GameServer
    /// </summary>
    public Dictionary<Guid, ClientData> ConnectedClients { get; }
    
    /// <summary>
    /// The Game servers ID
    /// </summary>
    public Guid ID { get; }
    
    /// <summary>
    /// The Status of the GameServer
    /// </summary>
    public GameStatus Status { get; set; }
    
    /// <summary>
    /// The GameState of the GameServer
    /// </summary>
    public GameStates GameState { get; set; }
    //Stores the questions available gathered from our API Call upon GameServer start
    public List<QuestionsModel> QList { get; set; }
    //Stores the categories available gathered from our API Call upon GameServer start
    public List<CategoriesModel> CList { get; set; }
    //Updates the current Questions active after they get removed
    public List<QuestionsModel> CQList { get; set; }
    //Is used for storing the choice of the place to find the correct question list
    public string CategoryChosen { get; set; }
    //Is used for storing the id of chosen question 
    public int QuestionChosen { get; set; }
    //Stores the attempts to answer the current question before removing it 
    public int AnswerAttempts { get; set; }
    
    /// <summary>
    /// Checks if the GameServer has had any players .. Used to control when the server can close on RunTime
    /// </summary>
    public bool HasHadPlayers { get;}

    /// <summary>
    /// Starts the GameServer
    /// </summary>
    public void StartGame();
    
    /// <summary>
    /// Updates the GameServer
    /// </summary>
    public void UpdateGame();
    
    /// <summary>
    /// Close the GameServer
    /// </summary>
    public void Close();
    
    /// <summary>
    /// Adds a player to the GameServer
    /// </summary>
    /// <param name="client">The client to add</param>
    public void AddPlayer(ClientData client);
    
    /// <summary>
    /// Checks if the server is full
    /// </summary>
    /// <returns></returns>
    public bool IsFull();
    
    /// <summary>
    /// Disconnects a player from the GameServer
    /// </summary>
    /// <param name="client">The client to disconnect</param>
    public void DisconnectPlayer(ClientData client);

    /// <summary>
    /// Checks if the GameServer can close or not
    /// </summary>
    public void EmptyGameCheck();
}