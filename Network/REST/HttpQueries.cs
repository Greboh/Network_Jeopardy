using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebApplication1.Controllers;


namespace REST;

public class HttpQueries
{
    //Local Ip of Rest web application
    public string Url = "http://localhost:5000/api/JeopardyQuiz";
    //Client trying to get connection to the api
    public HttpClient Client;
    //Message type for sending data back to client 
    public HttpResponseMessage Response;
    
    public string ResponseBody;
    
    //Question generator class for base question creation on startup
    private QuestionGenerator _generateQuestion = new();
    
    private List<QuestionsModel> _questionToCreate = new();
    
    
    /// <summary>
    /// ListenUrl starts when the server is executed
    /// </summary>
    public async void ListenUrl()
    {
         var handler = new HttpClientHandler();
         handler.ClientCertificateOptions = ClientCertificateOption.Manual;
         handler.ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true;
        
         Client = new(handler);

         try
         {
            //Generates a List of Questions from a static string in QuestionGenerator.cs
             _questionToCreate =  _generateQuestion.CreateQuestions();
             //Request sent to API
             Response = await Client.GetAsync(Url);
             //awaiting data object to be sent back
             ResponseBody = await Response.Content.ReadAsStringAsync();
         }
         catch (Exception e)
         {
             Console.WriteLine(e);
             throw;
         }
        

    }


    /// <summary>
    /// Create question handles automatic generation of base questions if first time rest is started
    /// else it handles manuel question creations
    /// </summary>
    /// <param name="automatic"></param>
    public async void CreateQuestion(bool automatic)
    {
        string? Category = default;
        string? Question = default;
        string? Answer = default;
        if (!automatic)
        {
            Console.WriteLine("Enter Category of question:");
        
            Category = Console.ReadLine();
            Console.WriteLine("Enter Question:");
            Question = Console.ReadLine();
            Console.WriteLine("Enter Answer:");
            Answer = Console.ReadLine();
        }
        
        

        
        try
        {
            if (automatic)
            {
                foreach (QuestionsModel q in _questionToCreate)
                {
                    
                    StringContent data = new StringContent(JsonConvert.SerializeObject(q), Encoding.UTF8, "application/json");
                    HttpResponseMessage res = await Client.PostAsync(Url, data);
                } 
            }
            else
            {
                QuestionsModel question = new QuestionsModel(Category, Question, Answer);
                StringContent data = new StringContent(JsonConvert.SerializeObject(question), Encoding.UTF8, "application/json");
                HttpResponseMessage res = await Client.PostAsync(Url, data);
            }
            
            
            
            


            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    
    public async Task<List<QuestionsModel>> GetQuestions()
    {
        Response = await Client.GetAsync(Url);
        ResponseBody = await Response.Content.ReadAsStringAsync();
        
        List<QuestionsModel> QuestionList;

        try
        {

            QuestionList = JsonConvert.DeserializeObject<List<QuestionsModel>>(ResponseBody);
            
           



        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        
        return QuestionList;
        
    }
    
    public async void GetQuestionById(string id)
    {
        Response = await Client.GetAsync(Url + "/" + id  );
        ResponseBody = await Response.Content.ReadAsStringAsync();
        QuestionsModel question;
        try
        {
            
            //string SerializedCode = JsonConvert.SerializeObject(ResponseBody);
            // List<QuestionModelJson>? FromJson(string json)  => JsonConvert.DeserializeObject<List<QuestionModelJson>>(ResponseBody);
            question = JsonConvert.DeserializeObject<QuestionsModel>(ResponseBody);
            /* foreach (QuestionsModel q in quetionModels)
             {
                 Console.WriteLine(q.Question);
             }*/
            //Console.WriteLine(quetionModels);

            //Console.WriteLine(questionList.Count);
            
                // Console.Write("Id:");
                // Console.WriteLine(question.Id);
                //
                // Console.Write("Category:");
                // Console.WriteLine(question.Category);
                //
                // Console.Write("Question:");
                // Console.WriteLine(question.Question);
                //
                // Console.Write("Answer:");
                // Console.WriteLine(question.Answer);
            
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

    }
    
    
    
    
    public async Task<List<CategoriesModel>> GetCategories()
    {
        Response = await Client.GetAsync(Url+"/category");
        ResponseBody = await Response.Content.ReadAsStringAsync();
        List<CategoriesModel> CategoryList;
        try
        {
            
            CategoryList = JsonConvert.DeserializeObject<List<CategoriesModel>>(ResponseBody);
            // foreach (CategoriesModel c in CategoryList)
            // {
            //     // Console.Write("Category:");
            //     // Console.WriteLine(c.Category);
            // }


        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return CategoryList;

    }
    public async Task<List<QuestionsModel>>GetQuestionsByCategory(string category)
    {
        Response = await Client.GetAsync(Url + "/category/" + category  );
        ResponseBody = await Response.Content.ReadAsStringAsync();
        List<QuestionsModel> QuestionList;

        try
        {
            
            //string SerializedCode = JsonConvert.SerializeObject(ResponseBody);
            // List<QuestionModelJson>? FromJson(string json)  => JsonConvert.DeserializeObject<List<QuestionModelJson>>(ResponseBody);
            QuestionList = JsonConvert.DeserializeObject<List<QuestionsModel>>(ResponseBody);
            foreach (QuestionsModel q in QuestionList)
            {
                Console.Write("Id:");
                Console.WriteLine(q.Id);

                Console.Write("Category:");
                Console.WriteLine(q.Category);

                Console.Write("Question:");
                Console.WriteLine(q.Question);

                Console.Write("Answer:");
                Console.WriteLine(q.Answer);
            }
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return QuestionList;
    }


   
}


