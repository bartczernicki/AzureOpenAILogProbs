using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpenAILogProbs.DTOs
{
    public class Question
    {
        public int Number { get; set; }
        public bool EnoughInformationInProvidedContext { get; set; }
        public required string QuestionText { get; set; }
    }
}
