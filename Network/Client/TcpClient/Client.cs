using System.Net.Sockets;
using System.Text;
using AES_CBC;
using SharedLogic.Package;
using SharedLogic.Processor;
using SharedLogic.TcpHelper;
using WebApplication1.Controllers;

namespace Tcp.Client;

/// <summary>
/// Available commands to use in the console
/// </summary>
public enum Commands
{
    Welcome = 0,
    Help = 11,
    Clear = 12,
    Message = 14,
    Connect = 1,
    Join = 2,
    Start = 3,
    Exit = 7,
    Choice = 4
}

/// <summary>
/// Handles everything about the client
/// </summary>
public class Client
{
    #region Fields

    /// <summary>
    /// The TcpClient that connects to the server
    /// </summary>
    TcpClient _client;

    /// <summary>
    /// The stream that the client uses to read and write from
    /// </summary>
    NetworkStream _stream;

    /// <summary>
    /// The reader responsible for reading from the stream
    /// </summary>
    StreamReader _reader;

    /// <summary>
    /// The writer responsible for reading from the stream
    /// </summary>
    StreamWriter _writer;

    /// <summary>
    /// The struct that contains the clients username & password
    /// </summary>
    AccountInfo _account;

    /// <summary>
    /// Controls whether the client is started / connected / disconnected
    /// </summary>
    ClientConnectionStatus _status;

    /// <summary>
    /// Dictionary that holds all the commands & their description
    /// </summary>
    private Dictionary<Commands, string> _helpers = new()
    {
        {Commands.Welcome, "Shows the welcome text"},
        {Commands.Help, "Shows all available commands"},
        {Commands.Clear, "Clears console screen"},
        {Commands.Connect, "This is used to connect to the server, only works when you haven't connected to the server yet!"},
        {Commands.Message, "Enables you to write a message to all other players"},
        {Commands.Join, "This is used to join a game"},
        {Commands.Start, "If you are the first to join a game, this starts said game"},

        {Commands.Exit, "Exit's the console application"},
        {Commands.Choice, "Used to progress game states"}
    };

    /// <summary>
    /// Used for aes encryption and decryption
    /// </summary>
    private AesCrypto _aes = new();
    
    #endregion
    
    /// <summary>
    /// Constructor that makes sure the client gets a username & password
    /// </summary>
    /// <param name="username">Username of the client</param>
    /// <param name="password">Password of the client</param>
    public Client(string username, string password)
    {
        _account.username = username;
        _account.password = password;
        _client = new();

        // Start the client
        Start();
    }

    #region Client methods

    /// <summary>
    /// Handles starting the client
    /// </summary>
    private void Start()
    {
        WelcomeText();

        while (_status == ClientConnectionStatus.Started)
        {
            ProcessClientInput();
        }
    }

    /// <summary>
    /// Tries to connect the client to the server
    /// </summary>
    private void TryConnectClient()
    {
        try
        {
            Console.WriteLine($"Connecting ..");
            
            // Connects the client to the server
            _client.Connect(ConnectionHelper.ip, ConnectionHelper.port);
            
            // Sets the stream to the client's
            _stream = _client.GetStream();
            
            // Create StreamReader & StreamWriter
            _reader = new(_stream, Encoding.UTF8);
            _writer = new(_stream, Encoding.UTF8);

            // Now we have everything needed to send our AccountInfo to the server
            SendAccountInfo();

            // Now the server has fully processed us and we can then update
            Update();
        }
        // If we cannot connect catch it
        catch (Exception ex)
        {
            // Write error handling message
            Write("<=Red>Unable</> to connect to server. Please try again or '<=Red>exit</>'\n");
        }
    }

    /// <summary>
    /// Functions like a regular Update method
    /// Using a while loop to keep the thread alive
    /// </summary>
    private void Update()
    {
        // Clear console for visibility
        Console.Clear();
        
        // Set our status to connected
        _status = ClientConnectionStatus.Connected;
        
        // Tell the client they have successfully connected
        Write("You have <=Green>successfully</> connected to the server\nYou can either type <=Yellow>Join</> To join a game or hangout in the lobby!\n");

        
        // Create & start a thread that reads packages from server
        Thread readPackagesThread = new(PackageReader) {IsBackground = true};
        readPackagesThread.Start();

        // While we are connected handle input from the client
        while (_status == ClientConnectionStatus.Connected) { ProcessClientInput(); }
    }

    /// <summary>
    /// String manipulation that detects the clients input and calls the relevant method
    /// </summary>
    private void ProcessClientInput()
    {
        // Read the clients input
        string command = Console.ReadLine();
        
        // Make sure the string is not null or empty 
        if (command.Length == 0 || string.IsNullOrEmpty(command)) return;

        // Always capitalize first index - style matters .. and it makes it easier to know if the string starts with upper- or lowercase 
        command = $"{command[0].ToString().ToUpper()}{command[1..]}";

        // Make ? act as help command
        if (command == "?") command = "Help";

        // If client types something that isn't a command return .. 
        if (!Enum.TryParse<Commands>(command, out var result))
        {
            Write("<=Red>Error</> Command not found!\nType '<=Yellow>?</>' or '<=Yellow>Help</>' for a list of '<=Yellow>commands</>'\n");
            return;
        }

        // Switch on result, calling a method depending on it
        switch (result)
        {
            case Commands.Welcome:
                WelcomeText();
                break;

            case Commands.Help:
                Help();
                break;

            case Commands.Clear:
                Console.Clear();
                break;

            case Commands.Connect:
                TryConnectClient();
                break;

            case Commands.Message:
                SendBroadcastPackage();
                break;

            case Commands.Join:
                SendJoinGameMessage();
                break;

            case Commands.Start:
                SendStartGameMessage();
                break;

            case Commands.Exit:
                Exit();
                break;
            case Commands.Choice:
                SendGameChoice();
                break;

            default:
                Console.WriteLine("This command does not exist! try again");
                break;
        }
    }

    /// <summary>
    /// When the client wants to connect / exit this method gets called
    /// </summary>
    private void Exit()
    {
        // Writes exit response
        Write("<=Red>Exiting ..</>\n");
        
        // Sleeps for a second, just to make it smoother
        Thread.Sleep(1000);
        
        // Set status to disconnected (Exits the various while loops)
        _status = ClientConnectionStatus.Disconnected;
    }

    #endregion

    #region Send Package Methods
    
    // A lot of the methods under this region does the same
    // A few will be commented but others wont since the they are almost identical

    /// <summary>
    /// Creates a packages and converts it into a string and then sends it to the NetworkStream
    /// </summary>
    private void SendAccountInfo()
    {
        // The package we wish to send in a string
        string clientDataPackage = PackageProcessor.CreateAccountInfoPackage(_account);
        
        // Encrypting the string using aes
        string encryptedMessage = _aes.Encrypter(clientDataPackage);
        
        // Make sure that the string contains anything
        if (clientDataPackage.Length <= 0) return;

        // Write the string to the NetworkStream
        ConnectionHelper.WriteToStream(_writer, encryptedMessage);
    }

    /// <summary>
    /// Creates a broadcast message and converts it into a string & and then sends it to the NetworkStream
    /// Is only called by using the associated command!
    /// </summary>
    private void SendBroadcastPackage()
    {
        // Therefor it can only be used if the client is connected
        if (_status != ClientConnectionStatus.Connected)
        {
            Write("<=Red>Error</> Please connect to the server first!\n");
            return;
        }

        Console.WriteLine($"Please write your message!");
        string messagePackage = PackageProcessor.CreateBroadcastMessage($"{Console.ReadLine()}");
        string encryptedMessage = _aes.Encrypter(messagePackage);
        if (messagePackage.Length <= 0) return;

        ConnectionHelper.WriteToStream(_writer, encryptedMessage);
    }
    
    private void SendJoinGameMessage()
    {
        if (_status != ClientConnectionStatus.Connected)
        {
            Write("<=Red>Error</> Please connect to the server first!\n");
            return;
        }

        string messagePackage = PackageProcessor.CreateBroadcastMessage("Join");
        string encryptedMessage = _aes.Encrypter(messagePackage);
        if (messagePackage.Length <= 0) return;

        ConnectionHelper.WriteToStream(_writer, encryptedMessage);
    }

    private void SendStartGameMessage()
    {
        if (_status != ClientConnectionStatus.Connected)
        {
            Write("<=Red>Error</> Please connect to the server first!\n");
            return;
        }

        string messagePackage = PackageProcessor.CreateBroadcastMessage("Start");
        string encryptedMessage = _aes.Encrypter(messagePackage);
        if (messagePackage.Length <= 0) return;

        ConnectionHelper.WriteToStream(_writer, encryptedMessage);
    }

    private void SendGameChoice()
    {
        if (_status != ClientConnectionStatus.Connected)
        {
            Write("<=Red>Error</> Please connect to the server first!\n");
            return;
        }

        Console.WriteLine($"Please write your Choice!");
        string messagePackage = PackageProcessor.CreateGameChoicePackage($"{Console.ReadLine()}");
        string encryptedMessage = _aes.Encrypter(messagePackage);
        if (messagePackage.Length <= 0) return;

        ConnectionHelper.WriteToStream(_writer, encryptedMessage);
    }

    #endregion

    #region Read Package Methods

    /// <summary>
    /// Responsible for reading all the packages that the client can receive
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private void PackageReader()
    {
        // Only try to read packages if our status is connected
        while (_status == ClientConnectionStatus.Connected)
        {
            try
            {
                // If there is no data in the stream, dont try to read anything!
                if (!_stream.DataAvailable) continue;

                // Get Package from stream
                string? packageInfo = _reader.ReadLine();

                string decryptedPackage = _aes.Decrypter(packageInfo);
                // Create Package from packageInfo
                NetworkPackage package = PackageProcessor.ReadPackage(decryptedPackage)!;
                
                // Console.WriteLine($"\nReceived {package}");

                // Store the type and data of the package is references, to make more readable
                PackageType type = package.type;
                PackageData data = package.data;

                switch (type)
                {
                    case PackageType.Broadcast:
                        ReadMessage(data);
                        break;
                    case PackageType.Echo:
                        ReadEcho(data);
                        break;
                    case PackageType.GameMessage:
                        ReadGameMessage(data);
                        break;
                    case PackageType.Categories:
                        ReadCategories(data);
                        break;
                    case PackageType.Questions:
                        ReadCategoryQuestions(data);
                        break;
                    case PackageType.QuestionsQuestion:
                        ReadQuestion(data);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"This packaged hasn't been added to the client PackageProcessor");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }

    // A lot of the methods under this region does the same
    // A few will be commented but others wont since the they are almost identical
    
    private void ReadMessage(PackageData? data)
    {
        // Create a string that stores the PackageData's message
        var message = (data as MessagePackage).Message;
        
        // Write this message to the console
        Console.WriteLine(message);
    }

    private void ReadEcho(PackageData data)
    {
        var message = (data as EchoPackage).Echo;
        Console.WriteLine(message);
    }

    private void ReadGameMessage(PackageData? data)
    {
        var message = (data as GameMessagePackage).Message;
        Console.WriteLine(message);
    }

    private void ReadCategories(PackageData? data)
    {
        // Get the categories sent from the server
        List<CategoriesModel>? categories = (data as CategoriesPackage)?.Categories;

        // Make sure that the list isn't 0 .. Guard clause
        if (categories.Count == 0) return;
        
        Console.WriteLine("Enter name of Category you wish to choose:");
       
        // Write all the categories to the console
        foreach (CategoriesModel c in categories)
        {
            if (c.empty == false)
            {
                Console.WriteLine(c.Category);
            }
        }
    }

    private void ReadCategoryQuestions(PackageData data)
    {
        List<QuestionsModel> questions = (data as CategoryQuestionsPackage).Questions;

        if (questions.Count <= 0) return;
        
        Console.WriteLine("Enter ID of Question you wish to choose:");
        foreach (QuestionsModel q in questions)
        {
            Console.WriteLine($"{q.Id}");
        }
    }

    private void ReadQuestion(PackageData data)
    {
        QuestionsModel question = (data as QuestionPackage).Question;
        
        Console.WriteLine($"Category > {question.Category}");
        Console.WriteLine($"Chosen QuestionID > {question.Id}\nChosen Question > {question.Question}");
    }

    #endregion

    #region Console Fun

    /// <summary>
    /// The text that explains how the program works and the available commands 
    /// </summary>
    private void WelcomeText()
    {
        Console.Clear();
        Write("<=Red>Welcome</>!\nFirst a quick how-to guide to the client interface!\n");
        Write("You are not connected to the server quite yet!. To do this type '<=Yellow>Connect</>'\n");
        Write("At any point you can type either '<=Yellow>?</>' or '<=Yellow>Help</>' for a list of commands and their description\n");
    }

    /// <summary>
    /// Used to write all commands and their description to the console
    /// </summary>
    private void Help()
    {
        foreach (KeyValuePair<Commands, string> helper in _helpers)
        {
            Write($"\n<=Yellow>{helper.Key}</> <=Yellow> : </> <=Green>{helper.Value}</>");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Used write and color the text written to console
    /// </summary>
    /// <param name="text">The text that should be written to the console</param>
    private void Write(string text)
    {
        // Used to split the text by using < >
        string[] array = text.Split('<', '>');
        
        // go through all strings in the array
        foreach (var str in array)
        {
            // Used to find where we want to change the color
            if (str.StartsWith("=") && Enum.TryParse(str.Substring(1), out ConsoleColor color))
            {
                Console.ForegroundColor = color;
            }
            // Used to determine where we want to change the color back to default (black) 
            else if (str.StartsWith("/"))
            {
                Console.ResetColor();
            }
            // else write the text as usual
            else Console.Write(str);
        }
    }

    #endregion
}