using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpenAILogProbs
{
    enum ProcessingOptions
    {
        None = 0,
        FirstTokenProbability = 1,
        FirstTokenProbabilityWithBrierScore = 2,
        WeightedProbability = 3,
        ConfidenceInterval = 4
    }
}
