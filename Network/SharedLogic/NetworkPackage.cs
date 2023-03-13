using SharedLogic.TcpHelper;
using WebApplication1.Controllers;

namespace SharedLogic.Package;

/// <summary>
/// The different PackageTypes that is sent and received 
/// </summary>
public enum PackageType
{
    AccountInfo = 0,
    Broadcast = 1,
    Echo = 2,
    GameMessage = 3,
    QuestionsQuestion = 4,
    Categories = 5,
    Questions = 6,
    GameChoice = 7,
}

/// <summary>
/// The main Package that is sent and received
/// Stores the type and the data of the package
/// </summary>
public class NetworkPackage
{
    public PackageType type;
    public PackageData? data;

    public override string ToString()
    {
        return
            "Package:\n" +
            $"Type > {type}\n" +
            $"Data > \n{data}\n";
    }
}

/// <summary>
/// Abstract class that all Packages inherit from
/// </summary>
[Serializable]
public abstract class PackageData
{
}

/// <summary>
/// PackageData storing the account info of the client
/// </summary>
public class AccountInfoPackage : PackageData
{
    public AccountInfo accountInfo { get; init; }
    
    public override string ToString()
    {
        return
            $"Username > {accountInfo.username}\n" +
            $"Password > {accountInfo.password}\n";
    }
}

/// <summary>
/// PackageData that stores the message from client / server
/// </summary>
public class MessagePackage : PackageData
{
    public string? Message { get; init; }

    public override string ToString()
    {
        return $"Broadcast > {Message}";
    }
}

/// <summary>
/// PackageData that stores the message that the server got
/// Used if the server wants to send the exact same message to others
/// </summary>
public class EchoPackage : PackageData
{
    public string Echo { get; init; }

    public override string ToString()
    {
        return $"Echo > {Echo}";
    }
}

/// <summary>
/// PackageData that stores the GameMessage which only the "GameServer" sends
/// </summary>
public class GameMessagePackage : PackageData
{
    public string Message { get; init; }

    public override string ToString()
    {
        return $"GameMessage > {Message}";
    }
}

/// <summary>
/// PackageData that stores the the GameChoice the client has taken
/// </summary>
public class GameChoicePackage : PackageData
{
    public string Message { get; init; }
}

/// <summary>
/// Object holds a list of categoriesModel which takes in the category string
/// aswell as if it has questions residing in its category for game mechanics purpose
/// if it does not have questions residing in the category name upon filter by category
/// in question list it should return categoriesModel.Empty = true;
/// </summary>
public class CategoriesPackage : PackageData
{
    public List<CategoriesModel>? Categories { get; init; }
}

/// <summary>
/// Object holds a list of questions
/// this is mainly to deliver the list of questions residing within
/// the category name upon filter for question by category
/// </summary>
public class CategoryQuestionsPackage : PackageData
{
    public List<QuestionsModel>? Questions { get; init; }
}

/// <summary>
/// Object holds simple questionModel that has fields
/// as Id, Category, Question, Answer
/// </summary>
public class QuestionPackage : PackageData
{
    public QuestionsModel? Question { get; init; }
    
}