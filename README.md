## Azure OpenAI LogProbs Examples  
   * .NET Console application that shows an implementation of Azure OpenAI Log Probs that can be useful for RAG implemenations:
     * Calculate First Token Probability - True or False probability, whether the (LLM) model has enough info to answer question  
     * Weighted Probability of Confidence Score - Self Confidence Score that is weighted from LogProbs PMF distribution to give a better confidence score estimate of confidence to answer a question
     * Confidence Interval - Calculated from bootstrap simulation of multiple calls to the model. This provides a 96% confidence interval (range) of plausible confidence scores. This is ideal when you need to understand a range of possibilities the model interprets rather than a single point estimate.

![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/Images/AzureLogProbsConsoleApp.png)

## Get Started: Add this to the User Secrets (Right Click on Project -> Manage User Secrets)
```javascript
{
  "AzureOpenAI": {
    "ModelDeploymentName": "gpt-4-0125-preview", // Any Azure OpenAI GPT-4 model should perform well
    "APIKey": "YOURAZUREOPENAIKEY",
    "Endpoint": "https://YOURAZUREOPENAIENDPOINT.openai.azure.com/"
  }
}
```

![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/Images/ProcessOption-FirstTokenProbability.png)  

![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/Images/ProcessOption-ConfidenceScoreWeightedProbability.png)  

![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/Images/ProcessOption-ConfidenceScoreInterval.png)  
