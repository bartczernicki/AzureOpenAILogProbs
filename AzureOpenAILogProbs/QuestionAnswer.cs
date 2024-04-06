﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpenAILogProbs
{
    internal class QuestionAnswer
    {
        public int Number { get; set; }
        public bool ExpectedAnswer { get; set; }
        public bool Answer { get; set; }
        public bool DoesAnswerMatchExpectedAnswer { get; set; }
        public double AnswerProbability { get; set; }
        public double BrierScore
        { get => 
                Math.Round(Math.Pow(AnswerProbability - Convert.ToDouble(DoesAnswerMatchExpectedAnswer), 2), 6);
        }
    }
}
