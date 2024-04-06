using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpenAILogProbs
{
    public class Question
    {
        public int Number { get; set; }
        public bool EnoughInformationInProvidedContext { get; set; }
        public string QuestionText { get; set; }
    }
}
