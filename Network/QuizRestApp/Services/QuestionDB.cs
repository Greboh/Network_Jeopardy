using WebApplication1.Controllers;

namespace WebApplication1.Services;

//local DB of questions by their ID
public class QuestionDb : Dictionary<int, QuestionsModel>
 {
    public static List<string> _activeCategories = new List<string>();
}
//local DB of Categoryes by their id = name
public class CategoryDB : Dictionary<string, CategoriesModel>
{
    
}

