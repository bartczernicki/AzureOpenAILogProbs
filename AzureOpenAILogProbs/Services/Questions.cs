using AzureOpenAILogProbs.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpenAILogProbs.Services
{
    public static class Questions
    {
        public static string GetContextForQuestions()
        {
            // Source: https://en.wikipedia.org/wiki/New_York_Mets 
            var sampleWikipediaArticle = """
                The New York Mets are an American professional baseball team based in the New York City borough of Queens.
                The Mets compete in Major League Baseball (MLB) as a member of the National League (NL) East Division.
                They are one of two major league clubs based in New York City, the other being the American League's (AL) New York Yankees.
                One of baseball's first expansion teams, the Mets were founded in 1962 to replace New York's departed NL teams, the Brooklyn Dodgers and the New York Giants.
                The team's colors evoke the blue of the Dodgers and the orange of the Giants.
                For the 1962 and 1963 seasons, the Mets played home games at the Polo Grounds in Manhattan before moving to Queens.
                From 1964 to 2008, the Mets played their home games at Shea Stadium, named after William Shea, the founder of the Continental League, a proposed third major league, the announcement of which prompted their admission as an NL expansion team.
                Since 2009, the Mets have played their home games at Citi Field next to the site where Shea Stadium once stood.
                In their inaugural season, the Mets posted a record of 40–120, the worst regular-season record since MLB went to a 162-game schedule.
                The team never finished better than second-to-last in the 1960s until the "Miracle Mets" beat the Baltimore Orioles in the 1969 World Series, considered one of the biggest upsets in World Series history despite the Mets having won 100 games that season.
                The Mets have qualified for the postseason ten times, winning the World Series twice (1969 and 1986) and winning five National League pennants (most recently in 2000 and 2015), and six National League East division titles.
                Since 2020, the Mets have been owned by billionaire hedge fund manager Steve Cohen, who purchased the team for $2.4 billion.[10] As of 2023, Forbes ranked the Mets as the sixth most valuable MLB team, valued at $2.9 billion.
                As of the end of the 2023 regular season, the team's overall win–loss record is 4,727–5,075–8 (.482).
                """;

            return sampleWikipediaArticle;
        }

        public static List<DTOs.Question> GetQuestions()
        {
            var questions = new List<Question>
                {
                new Question{ Number = 1, EnoughInformationInProvidedContext = true, QuestionText = "When where the Mets founded?" },
                new Question{ Number = 2, EnoughInformationInProvidedContext = true, QuestionText = "Are the Mets a baseball team?" },
                new Question{ Number = 3, EnoughInformationInProvidedContext = true, QuestionText = "Who owns the Mets?" },
                new Question{ Number = 4, EnoughInformationInProvidedContext = false, QuestionText = "Have the Mets won the 2023 World Series?" },
                new Question{ Number = 5, EnoughInformationInProvidedContext = false, QuestionText = "Who are the Boston Red Sox?" },
                new Question{ Number = 6, EnoughInformationInProvidedContext = true, QuestionText = "Where the Mets were a bad team in the 1960s?" },
                new Question{ Number = 7, EnoughInformationInProvidedContext = true, QuestionText = "Do the Mets uniforms include the colors blue and orange?" },
                new Question{ Number = 8, EnoughInformationInProvidedContext = false, QuestionText = "Are the there only 2 colors on the Mets uniform?" },
                new Question{ Number = 9, EnoughInformationInProvidedContext = false, QuestionText = "Do you think the Mets fans are happy with the Mets historical performance?" },
                new Question{ Number = 10, EnoughInformationInProvidedContext = true, QuestionText = "Did the Mets have a winning season in their inaugural season of play?" },
                new Question{ Number = 11, EnoughInformationInProvidedContext = false, QuestionText = "Has Steve Cohen been the longest-serving owner of the Mets?" },
                new Question{ Number = 12, EnoughInformationInProvidedContext = false, QuestionText = "Is Citi Field located on the exact site of the old Shea Stadium?" }
                };

            return questions;
        }
    }
}
