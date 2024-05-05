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
