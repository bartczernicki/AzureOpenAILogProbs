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
            // Selected source content from Wikipedia Mets article: https://en.wikipedia.org/wiki/New_York_Mets 
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

                Mr. Met is the official mascot of the New York Mets. 
                He was introduced on the cover of game programs in 1963, when the Mets were still playing at the Polo Grounds in northern Manhattan. 
                When the Mets moved to Shea Stadium in 1964, fans were introduced to a live costumed version. 
                Mr. Met is believed to have been the first mascot in Major League Baseball to exist in human (as opposed to artistically rendered) form.

                The Mets have notable rivalries with the Atlanta Braves, the New York Yankees, and the Philadelphia Phillies. 
                The Braves rivalry is due to division realignment that put both teams in the National League East in 1994. 
                Their rivalry with the Yankees has its roots in the histories of the New York Giants, Brooklyn Dodgers, and the Yankees and the fierce Subway Series matchups between the two teams. 
                The rivalry with the Phillies stems from the geographic New York-Philadelphia rivalry, which is also seen in other sports.

                A registered 501(c)(3) charity, the New York Mets Foundation is the philanthropic organization of the New York Mets. 
                Founded in 1963, it funds and promotes charitable causes in the Mets community. 
                One of these causes is Tuesday's Children, is a non-profit family service organization that "has made a long term commitment to meet the needs of every family who lost a loved one in the terrorist attacks on September 11, 2001". 
                The Mets host the annual Welcome Home Dinner, which raised over $550,000 for the Mets Foundation in 2012. 
                All proceeds were distributed to Katz Institute for Women's Health and Katz Women's Hospitals of North Shore-LIJ Health System and The Leukemia & Lymphoma Society. 
                """;

            return sampleWikipediaArticle;
        }

        public static List<DTOs.Question> GetQuestions()
        {
            var questions = new List<Question>
                {
                new Question{ Number = 1, EnoughInformationInProvidedContext = true, QuestionText = "When where the Mets founded?" },
                new Question{ Number = 2, EnoughInformationInProvidedContext = true, QuestionText = "Are the Mets a baseball team or basketball team?" },
                new Question{ Number = 3, EnoughInformationInProvidedContext = true, QuestionText = "Who owns the Mets?" },
                new Question{ Number = 4, EnoughInformationInProvidedContext = false, QuestionText = "Have the Mets won the 2023 World Series?" },
                new Question{ Number = 5, EnoughInformationInProvidedContext = false, QuestionText = "Who are the Boston Red Sox?" },
                new Question{ Number = 6, EnoughInformationInProvidedContext = true, QuestionText = "Where the Mets were a bad team in the 1960s?" },
                new Question{ Number = 7, EnoughInformationInProvidedContext = true, QuestionText = "Do the Mets uniforms include what to primary colors?" },
                new Question{ Number = 8, EnoughInformationInProvidedContext = false, QuestionText = "Are the there only 2 colors on the Mets uniform?" },
                new Question{ Number = 9, EnoughInformationInProvidedContext = false, QuestionText = "Do you think the Mets fans are happy with the Mets historical performance?" },
                new Question{ Number = 10, EnoughInformationInProvidedContext = true, QuestionText = "What was the Mets record in their inaugural season of play?" },
                new Question{ Number = 11, EnoughInformationInProvidedContext = false, QuestionText = "Who has been the longest-serving owner of the Mets?" },
                new Question{ Number = 12, EnoughInformationInProvidedContext = true, QuestionText = "Who or what is the official mascot of the New York Mets?" },
                new Question{ Number = 13, EnoughInformationInProvidedContext = false, QuestionText = "Who or what is the official mascot of the New York Yankees?" },
                new Question{ Number = 14, EnoughInformationInProvidedContext = true, QuestionText = "Who are the three other baseball teams the New York Mets have notable rivalries with?" },
                new Question{ Number = 15, EnoughInformationInProvidedContext = false, QuestionText = $"The current year is: {DateTime.Now.Year}. Do you have a current year of the New York Met's worth?" },
                new Question{ Number = 16, EnoughInformationInProvidedContext = true, QuestionText = "The Mets current notable rivals the Phillies, Braves and which third team?" },
                new Question{ Number = 17, EnoughInformationInProvidedContext = false, QuestionText = "Is the Atlanta Braves rivalry due to both geographic and division reasons?" },
                new Question{ Number = 18, EnoughInformationInProvidedContext = true, QuestionText = "What is the Mets registered foundation's name?" },
                new Question{ Number = 19, EnoughInformationInProvidedContext = false, QuestionText = "What are the names of the Mets two foundations?" },
                new Question{ Number = 20, EnoughInformationInProvidedContext = false, QuestionText = "What is the Mets charity for both World War 2 veterans and September 11th attacks?" }
            };

            return questions;
        }
    }
}
