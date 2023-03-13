using Newtonsoft.Json;

namespace WebApplication1.Controllers;

public class QuestionsModel
{
    public int Id { get;  set; }
    public string Category { get; set; }
    public string Question { get; set; }
    public string Answer { get; private set; }
    
    public QuestionsModel(string category,string question, string answer)
    {
         Category = category;
         Question = question;
         Answer = answer;
    }


    public override string ToString()
    {
        return Question;
    }
}