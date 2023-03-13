namespace SharedLogic.TcpHelper;


/// <summary>
/// Connection status of the client
/// </summary>
public enum ClientConnectionStatus
{
    Started,
    Connected,
    Disconnected,
}

/// <summary>
/// Clients AccountInfo
/// </summary>
public struct AccountInfo
{
    public string username;
    public string password;
}

/// <summary>
/// Stores the basic networking methods used by both the client and server
/// </summary>
public static class ConnectionHelper
{
    /// <summary>
    /// The ip used
    /// </summary>
    public static string ip = "20.111.22.131";
    
    /// <summary>
    /// The port used
    /// </summary>
    public static int port = 13000;

    /// <summary>
    /// Writes and flushes data to the stream
    /// </summary>
    /// <param name="writer">The StreamWriter containing the stream and the character encoding used </param>
    /// <param name="message">The message to send </param>
    public static void WriteToStream(StreamWriter writer, string message)
    {
        // Write data to server
        writer.WriteLine(message);
        
        // Flush so it doesn't wait for the buffer to max
        writer.Flush();
    }
}