## Azure OpenAI LogProbs Examples  
   * .NET Console application that shows an implementation of Azure OpenAI Log Probs that can be useful for RAG implemenations:
     * Calculate First Token Probability - True or False probability, whether the (LLM) model has enough info to answer question  
     * Weighted Probability of Confidence Score - Self Confidence Score that is weighted from LogProbs PMF distribution to give a better confidence score estimate of confidence to answer a question
     * Confidence Interval - Calculated from bootstrap simulation of multiple calls to the model. This provides a 96% confidence interval (range) of plausible confidence scores. This is ideal when you need to understand a range of possibilities the model interprets rather than a single point estimate.

![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/Images/AzureLogProbsConsoleApp.png)

## Get Started: Add this to the User Secrets (Right Click on VS Project -> Manage User Secrets)
```javascript
{
  "AzureOpenAI": {
    "ModelDeploymentName": "gpt-4-0125-preview", // Any Azure OpenAI GPT-4 model should perform well
    "APIKey": "YOURAZUREOPENAIKEY",
    "Endpoint": "https://YOURAZUREOPENAIENDPOINT.openai.azure.com/"
  }
}
```
## Processing Options  

### 1) First Token Probability  
   * Uses the Azure OpenAI LogProbs to determine the probability of the first token in the response.
   * If the probability is high, it is likely the model has enough information to answer the question.  
   * If the probability is low, it is likely the model does not have enough information to answer the question.  
   * The probability can be used as a decision threshhold for a binary classification of whether the model has enough information (RAG context) to answer the question.     

Example Output:
![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/Images/ProcessOption-FirstTokenProbability.png)  

### 2) Weighted Probability of Confidence Score  
   * Azure OpenAI LogProbs can return a probability mass function (PMF) distribution of up to the next 5 tokens including their probabilities.  
   * This calculation uses multiple LogProbs to determine the "weighted" probability of the response.  
   * The wighted probability is calculated by multiplication: confidence score*probability to give a better weighted estimate of the confidence to answer the question.  
   * The weighted probability can be used as a better calibrated confidence score for the model's response.  

Note you have to enable LogProbs and set the LogProbabilitiesPerToken to 5 (max):  
```chsarp
chatCompletionOptionsConfidenceScore.Temperature = 0.0f;
chatCompletionOptionsConfidenceScore.EnableLogProbabilities = true;
// For the Confidence Score, we want to see 5 of the top log probabilities (PMF)
chatCompletionOptionsConfidenceScore.LogProbabilitiesPerToken = 5;
```  

Example Output:  
![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/Images/ProcessOption-ConfidenceScoreWeightedProbability.png)  


### 3) 95% Confidence Score Interval  
   * The previous examples show a single point estimate of the confidence score. This can be misleading as the model may have multiple interpretations of the response.  
   * Azure OpenAI LogProbs can return a probability mass function (PMF) distribution of up to the next 5 tokens including their probabilities.  
   * This calculation uses multiple LogProbs to determine the "confidence interval" of the response.  
   * The confidence interval is calculated by bootstrapping multiple calls (10) to the model and calculating the 95% confidence interval of the confidence scores.  
   * The confidence interval can be used to understand the range of possibilities, where 95% of the outcomes will fall within this range as the same question is repeated.  

Example Output:  
![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/Images/ProcessOption-ConfidenceScoreInterval.png)  
