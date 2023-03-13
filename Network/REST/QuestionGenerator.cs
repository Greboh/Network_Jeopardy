using WebApplication1.Controllers;

namespace REST;

public class QuestionGenerator
{
    private QuestionsModel _generateQuestion;

    public List<QuestionsModel> QuestionsList = new();


    public  List<QuestionsModel> CreateQuestions()
    {
        
        string questionData =
            "Arts,What Beatrix Potter character was rescued by a collie and two foxhounds?,Jemima Puddleduck;" +
            "Arts,What name is usually given to Quasimodo who features in a French novel first published in 1831?,The Hunchback of Notre Dame;" +
            "Arts,Who aimed his 'Emporio' clothing line at younger buyers?,Giorgio Armani;" +
            "Arts,Whose chest contained the 'Treasure Island' map?,Billy Bones;" +
            "Arts,Whose 'Small House' is the subject of the play acted out during 'The King and I'?,Uncle Thomas;" +
            "History,The 'St. Valentine's Day Massacre' was perpetrated by which historical figure?,Al Capone;" +
            "History,Under whose presidency were the waters in the fountains of the White House first dyed green in honor of Saint Patrick's Day?,Barack Obama;" +
            "History,In which ancient South Asian language is the text of The Vedas written?,Sanskrit;" +
            "History,Who are the first people known to have consumed chocolate?,Olmecs;" +
            "History,On 1 April 1946 which disaster struck Hawaii which people initially thought was a hoax?,Tsunami;" +
            "Science,What do most lobsters and crayfish do with their shell after it has molted?,Eat it;" +
            "Science,Which is the fourth planet from the Sun?,Mars;" +
            "Science,What two metals form the alloy white gold?,Gold and silver;" +
            "Science,What is the common name for butterfly larvae?,Caterpillars;" +
            "Science,What bird lays the smallest egg in relation to its size?,Ostrich;" +
            "Entertainment,Which famous music group were formerly known as the New Yardbirds?,Led Zeppelin;" +
            "Entertainment,What is the only videogame franchise originating in the 1970's to have thus far generated over $1 billion in revenue?,Space Invaders;" +
            "Entertainment,In the movie 'Polar Express' there is one rule when it comes to hot chocolate. What is it?,Don't let it cool;" +
            "Entertainment,What is the setting for the film 'It's a Wonderful Life'?,Bedford Falls;" +
            "Entertainment,Who ended his final 60 Minutes segment by saying 'if you do see me in a restaurant please just let me eat my dinner'?,Andy Rooney;" +
            "Geography,What is the capital of Guinea-Bissau?,Bissau;" +
            "Geography,In which continent would you find the Republic of Guinea-Bissau?,Africa;" +
            "Geography,Bissau is the capital of which West African country?,Guinea-Bissau;" +
            "Geography,The Republic of Guinea-Bissau is located on which African coast?,Western coast;" +
            "Geography,What year did the Republic of Guinea-Bissau declare independance from Portugal?,1973";


        string[] qSplit = questionData.Split(";");
        for (int i = 0; i < qSplit.Length; i++)
        {
            string[] qD = qSplit[i].Split(",");
            QuestionsList.Add(new QuestionsModel(qD[0],qD[1],qD[2]));
        }

        return QuestionsList;
    }

}