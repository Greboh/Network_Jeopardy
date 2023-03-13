using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[Route("api/[Controller]")]
[ApiController]
public class JeopardyQuizController : Controller
{
    private readonly QuestionDb _questionDb;
    private readonly CategoryDB _categoryDb;
    
    
    /// <summary>
    /// sets the readonly DB's for the controller via constructor
    /// </summary>
    /// <param name="questionDb"></param>
    /// <param name="categoriesDb"></param>
    public JeopardyQuizController(QuestionDb questionDb, CategoryDB categoriesDb)
    {
        _questionDb = questionDb;
        _categoryDb = categoriesDb;
    }

    //returns once the iteration has been for-filled 
    [HttpGet]
    public ActionResult<IEnumerable<QuestionsModel>> Get()
    {
        //returns the status of a list of objects containing question DB values 
        return Ok(_questionDb.Values);
    }

    //Get question by id
    [HttpGet("{id}")]
    public ActionResult<IEnumerable<int>> Get(int id)
    {
        //tries to find the value of a question by its id 
        if (_questionDb.TryGetValue(id, out QuestionsModel? question))
        {
            //returns status code of 200 if its found
            return Ok(question);
        }

        //returns status code of 404 if its not found
        return NotFound();
    }
    
    //returns once the iteration has been for-filled 
    [HttpGet("category")]
    public ActionResult<IEnumerable<CategoriesModel>> GetCategory()
    {
        //returns the status of a list of objects containing question DB values 
        // foreach value
        return Ok(_categoryDb.Values);
    }
    
    //Gets a list of object containing the question model matched upon their category 
    [HttpGet("category/{category}")]
    public ActionResult<IEnumerable<QuestionsModel>> Get(string category)
    {
        //returns a status of 200 when found
        return Ok(_questionDb.Values.Where(q => q.Category == category));
    }
    
    /// <summary>
    /// Create a question requires a model structure so Category, Question and id
    /// to reside in the json object sendt
    /// </summary>
    /// <param name="question"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult Post([FromBody] QuestionsModel? question)
    {
        if (question == null)
        {
            //if its nothing bad request status 400
            return BadRequest();
        }

        
        if (_questionDb.ContainsKey(_questionDb.Keys.Count + 1))
        {
            //if the id of question is already existing
            //returns status code 409
            return Conflict();
        }

        //checks if the question already exists in the question DB
        foreach (KeyValuePair<int ,QuestionsModel>  q in _questionDb)
        {
            if (q.Value.Question == question.Question)
            {
                //returns status code of 409
                return Conflict();
            }
        }
        
        
        //adds the category of question to a list of unique categories
        AddUniqueCategories(question);
        
        
        
        question.Id = _questionDb.Keys.Count + 1;
        _questionDb.Add(question.Id,question);
        //returns status code of 201 for creation complet
        return Created(Request.Path + "/" + question.Id, null);
    }

    // Delete question by ID 
    [HttpDelete("{id}")]
    public ActionResult<IEnumerable<QuestionsModel>> Delete(int id)
    {
       
        // if requested id to be deleted does not exist returns error of status 400
        if (!_questionDb.ContainsKey(id))
        {
            return BadRequest();
        }
        
        //stores the value of our question beeing looked for via DB
        OkObjectResult? deletedQuestion = Ok(_questionDb.Values.Where(q => q.Id == id));
        //removes the id from db
        _questionDb.Remove(id);
        //returns message of the question data deleted
        return deletedQuestion;
    }
    
    /// <summary>
    /// Adds a category of a question if its unique to _activeCategories list in db
    /// </summary>
    /// <param name="question"></param>
    private void AddUniqueCategories(QuestionsModel question)
    {
        _categoryDb.TryAdd(question.Category, new CategoriesModel(question.Category));
        QuestionDb._activeCategories.Add(question.Category);

    }

}