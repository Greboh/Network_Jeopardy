using Newtonsoft.Json;

namespace WebApplication1.Controllers;

public class CategoriesModel
{
    public string Category { get; private set; }
    public bool empty = false;
    public CategoriesModel(string category)
    {
        Category = category;
    }
    
    
}

