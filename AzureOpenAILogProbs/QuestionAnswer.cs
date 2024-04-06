using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpenAILogProbs
{
    internal class QuestionAnswer
    {
        public int Number { get; set; }
        public bool DoesAnswerMatchExpectedAnswer { get; set; }
        public bool Answer { get; set; }
        public double AnswerProbability { get; set; }
        public double BrierScore { get => Math.Pow(AnswerProbability - Convert.ToDouble(DoesAnswerMatchExpectedAnswer), 2); }
    }
}
