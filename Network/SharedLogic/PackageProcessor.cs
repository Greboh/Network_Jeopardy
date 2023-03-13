using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedLogic.Package;
using SharedLogic.TcpHelper;
using WebApplication1.Controllers;

namespace SharedLogic.Processor;


/// <summary>
/// This class handles reading and writing packages to the client / server
/// </summary>
public static class PackageProcessor
{
    
    #region NetworkPackage Write Methods
    
    // A lot of the methods under this region does the same
    // A few will be commented but others wont since the they are almost identical
    
    /// <summary>
    /// Creates the AccountInfo package
    /// </summary>
    /// <param name="accountInfo">The AccountInfo that the package sends</param>
    /// <returns>Returns the string used to encrypt and send to server</returns>
    public static string CreateAccountInfoPackage(AccountInfo accountInfo)
    {
        // Creates a new package setting the proper type & data
        NetworkPackage package = new()
        {
            type = PackageType.AccountInfo,
            data = new AccountInfoPackage { accountInfo = accountInfo}
        };

        // Returns the package serialized to Json 
        return JsonConvert.SerializeObject(package);
    }

    public static string CreateBroadcastMessage(string message)
    {
        NetworkPackage package = new()
        {
            type = PackageType.Broadcast,
            data = new MessagePackage {Message = message}
        };

        return JsonConvert.SerializeObject(package);
    }

    public static string CreateEchoPackage(string echo)
    {
        NetworkPackage package = new()
        {
            type = PackageType.Echo,
            data = new EchoPackage {Echo = echo}
        };
        
        return JsonConvert.SerializeObject(package);
    }
    
    public static string CreateGameMessagePackage(string message)
    {
        NetworkPackage package = new()
        {
            type = PackageType.GameMessage,
            data = new GameMessagePackage {Message = message}
        };
            
        return JsonConvert.SerializeObject(package);
    }

    public static string CreateGameChoicePackage(string choice)
    {
        NetworkPackage package = new()
        {
            type = PackageType.GameChoice,
            data = new GameChoicePackage {Message = choice}
        };
            
        return JsonConvert.SerializeObject(package);
    }
    
    public static string CreateQuestionPackage(QuestionsModel questionData)
    {
        NetworkPackage package = new NetworkPackage
        {
            type = PackageType.QuestionsQuestion,
            data = new QuestionPackage {Question = questionData}
        };

        return JsonConvert.SerializeObject(package);;
    }
    
    public static string CreateCategoriesPackage(List<CategoriesModel> categoriesData)
    {
        NetworkPackage package = new NetworkPackage
        {
            type = PackageType.Categories,
            data = new CategoriesPackage() {Categories = categoriesData}
        };

        return JsonConvert.SerializeObject(package);;
    }
    
    public static string CreateQuestionsPackage(List<QuestionsModel> questionData)
    {
        NetworkPackage package = new NetworkPackage
        {
            type = PackageType.Questions,
            data = new CategoryQuestionsPackage() {Questions = questionData}
        };

        return JsonConvert.SerializeObject(package);;
    }
    
    
    #endregion

    #region NetworkPackage Read Methods

    /// <summary>
    /// Responsible for reading the package from the client / server
    /// </summary>
    /// <param name="packageString">The string that needs to be read</param>
    /// <returns>Returns the NetworkPackage from the packageString</returns>
    public static NetworkPackage? ReadPackage(string? packageString)
    {
        // Create a null package used for return value
        NetworkPackage? package = null;

        // Parse the package string in order to get the whole package object
        JObject? packageObject = JObject.Parse(packageString);
        
        // Get the JToken that is associated with "type" this will get us the packageType
        JToken? typePackage = packageObject["type"];

        // Make sure the type is something
        if (typePackage == null) return package;

        // Get the int value of the type, this is a safe cast so no need for null checking
        PackageType type = (PackageType) typePackage.Value<int>();
        
        // Get the JToken that is associated with "data" this will get us the PackageData
        JToken? basePackage = packageObject["data"];

        // Make sure the data is something
        if (basePackage == null) return null;

        // Switch on the packageType so we can handle them differently
        
        // These cases are mostly the same.. Only the first will be commented
        switch (type)
        {
            case PackageType.AccountInfo:
                // Get the basePackage which in this case is of type AccountInfoPackage
                AccountInfoPackage? accountPackage = basePackage.ToObject<AccountInfoPackage>();
                
                // Set our package variable created at the top of this method to a new Package containing our type and ata
                package = new() {type = PackageType.AccountInfo, data = accountPackage};
                break;

            case PackageType.Broadcast:
                MessagePackage? messagePackage = basePackage.ToObject<MessagePackage>();
                package = new() {type = PackageType.Broadcast, data = messagePackage};
                break;
                
            case PackageType.Echo:
                EchoPackage? echoPackage = basePackage.ToObject<EchoPackage>();
                package = new() {type = PackageType.Echo, data = echoPackage};
                break;
            
            case PackageType.GameMessage:
                GameMessagePackage? gameMessagePackage = basePackage.ToObject<GameMessagePackage>();
                package = new() {type = PackageType.GameMessage, data = gameMessagePackage};
                break;
            case PackageType.GameChoice:
                GameChoicePackage? gameChoicePackage = basePackage.ToObject<GameChoicePackage>();
                package = new() {type = PackageType.GameChoice, data = gameChoicePackage};
                break;
            case PackageType.Categories:
                CategoriesPackage? categoriesPackage = basePackage.ToObject<CategoriesPackage>();
                package = new() {type = PackageType.Categories, data = categoriesPackage};
                break;
            case PackageType.Questions:
                CategoryQuestionsPackage? categoriesChoicePackage = basePackage.ToObject<CategoryQuestionsPackage>();
                package = new() {type = PackageType.Questions, data = categoriesChoicePackage};
                break;
            case PackageType.QuestionsQuestion:
                QuestionPackage? questionPackage = basePackage.ToObject<QuestionPackage>();
                package = new() {type = PackageType.QuestionsQuestion, data = questionPackage};
                break;
        }
        
        // Return the package read from the string
        return package;
    }

    #endregion
}