using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using SharedLogic.TcpHelper;

namespace SharedLogic;

/// <summary>
/// Stores all values needed for the server to differentiate between the clients
/// </summary>
public class ClientData
{
    /// <summary>
    /// Username of the client
    /// </summary>
    public string Username { get; private set; }
    
    /// <summary>
    /// Password of the client
    /// </summary>
    public string Password { get; private set; }
    
    /// <summary>
    /// The TcpClient that is associated with this client
    /// </summary>
    public TcpClient Tcp { get; init; }
    
    /// <summary>
    /// The NetworkStream that is associated with this client
    /// </summary>
    public NetworkStream ClientStream { get; init; }
    
    /// <summary>
    /// The IPEndPoint that is associated with this client
    /// </summary>
    public IPEndPoint IP { get; init; }
    
    /// <summary>
    /// A Guid that only this client has, used in a dictionary to as the key
    /// </summary>
    public Guid NetworkID { get; } = Guid.NewGuid();
    
    /// <summary>
    /// The StreamReader that is associated with this client
    /// </summary>
    public StreamReader Reader { get; init; }
    
    /// <summary>
    /// The StreamWriter that is associated with this client
    /// </summary>
    public StreamWriter Writer { get; init; }
    
    /// <summary>
    /// The Clients connectionStatus that is associated with this client
    /// </summary>
    public ClientConnectionStatus Status { get; set; }
    
    /// <summary>
    /// The bool that controls if the client has power of the GameServer
    /// </summary>
    public bool IsGameOwner { get; set; }
    
    /// <summary>
    /// Used to set the remaining data that the server receives from the AccountInfoPackage
    /// </summary>
    /// <param name="username">Username of client</param>
    /// <param name="password">Password of client</param>
    public void SetRemainingData(string username, string password)
    {
        Username = username;
        Password = password;
        
        // Set his status to connected now
        Status = ClientConnectionStatus.Connected;
    }

    public override string ToString()
    {
        return $"Client Info:\n" +
               $"Username > {Username}\n" +
               $"IP > {IP}\n" +
               $"NetworkID > {NetworkID}\n";
    }
}