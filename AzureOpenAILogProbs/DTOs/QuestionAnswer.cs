using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpenAILogProbs.DTOs
{
    internal class QuestionAnswer
    {
        public int Number { get; set; }
        public bool ExpectedAnswer { get; set; }
        public bool LLMAnswer { get; set; }
        public bool DoesLLMAnswerMatchExpectedAnswer { get; set; }
        public double AnswerProbability { get; set; }
        public double BrierScore
        {
            get =>
                Math.Round(Math.Pow(AnswerProbability - Convert.ToDouble(DoesLLMAnswerMatchExpectedAnswer), 2), 6);
        }
    }
}
