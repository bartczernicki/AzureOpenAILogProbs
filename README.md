## Azure OpenAI LogProbs Examples  
   * .NET Console application that shows examples how Azure OpenAI Log Probs that can be useful for RAG implementations:
     * Calculate First Token Probability - True or False probability, determine whether the (LLM) model has enough info to answer question  
     * Weighted Probability of Confidence Score - Self Confidence Score that is weighted from LogProbs probability (PMF) distribution to give a better (weighted) confidence score estimate to answer a question
     * Confidence Interval - Calculated from bootstrap simulation of multiple calls to the model. This provides a 95% confidence interval (range) of plausible confidence scores. This is ideal when you need to understand a range of possibilities the model interprets rather than a single point estimate.

![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/AzureLogProbsConsoleApp.png)

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
## Background Information  

What are LogProbs? Most current LLMs process prompt instructions by predicting the next token and iterate through each token until they reach a stopping point (i.e. max token length, completing the thought). Each next token that is considered for output is calculated through statistical probability distribution that are calulated from the logarithm of probabilities (logprobs). Based on configurations (tempature, top_p etc.) these probabilities can be calibrated and then the LLM selects the next "best token" based on the different configurations. Because these LLMs are probabilistic in nature, this is why you may see different outputs for the same question to the LLM. Below is an example of a question and answer and the associated probabilities for the two tokens/words and the ones that were selected: "George Washington".

![Azure LogProbs Example](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/AzureLogProbs-Example.png)

Recommended Reading on the background of Azure OpenAI LogProbs:  
   * OpenAI Cookbook - LogProbs: https://cookbook.openai.com/examples/using_logprobs  
   * What are logprobs?: https://www.ignorance.ai/p/what-are-logprobs  

The three examples illustrated focus on reducing hallucinations and improving the reliability of the model's responses when presented with grounding information and a question.
There are several emerging techniques that use multiple calls to a model or several models to arrive at a response, conclusion or a decision.
A plurality of ways LLMs are used in GenAI production systems is with grounding (RAG) with additional context.
The model is asked to answer a question, reason over that information etc. However, with poor grounding, this can result in poor results.  

Azure OpenAI LogProbs are a tool that can help and be used to gauge the confidence (probability) of the model's response.
This tremendous capability can empower the AI system to self-correct or guide the user/agent to arrive at a better response.
In the set of examples below, we will simulate a parallel call to the model to gauge the confidence the model has with the presented context and question.
This is illustrated below with the diagram:  

![Azure LogProbs Workflow](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/AzureLogProbs-LogProbsWorkflow.png)  


## Console Processing Options  

### 1) First Token Probability - How Confident is the AI Model with the information to answer the question  
   * Uses the Azure OpenAI LogProbs to determine the probability of the first token in the response.
   * If the probability is high, it is likely the model has enough information to answer the question.  
   * If the probability is low, it is likely the model does not have enough information to answer the question.  
   * The probability can be used as a decision threshold for a binary classification of whether the model has enough information (RAG context) to answer the question.     

Example Output:
![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/ProcessOption-FirstTokenProbability.png)  

### 2) Weighted Probability of Confidence Score - Model provides a self-confidence score and then assess the probability of the confidence score
   * Azure OpenAI LogProbs can return a probability mass function (PMF) distribution of up to the next 5 tokens including their probabilities.  
   * This calculation uses multiple LogProbs to determine the "weighted" probability of the response.  
   * The weighted probability is calculated by multiplication: confidence score*probability to give a better weighted estimate of the confidence to answer the question.  
   * The weighted probability can be used as a better calibrated confidence score for the model's response.  

Note you have to enable LogProbs and set the LogProbabilitiesPerToken to 5 (current maximum, as of this writing):  
```chsarp
chatCompletionOptionsConfidenceScore.Temperature = 0.0f;
chatCompletionOptionsConfidenceScore.EnableLogProbabilities = true;
// For the Confidence Score, we want to see 5 of the top log probabilities (PMF)
chatCompletionOptionsConfidenceScore.LogProbabilitiesPerToken = 5;
```  

Example Output:  
![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/ProcessOption-ConfidenceScoreWeightedProbability.png)  

<p align="left" width="100%">
    Below is an example of a Token Probability Distribution when 5 LogProbs tokens are returned with their respective probabilities. In the histogram below, "Confidence Score: 1" has a 42.3% probability; which means the model thinks that is has a very low Confidence Score=1 of answering the question and that low chance is 42.3%. If you just select that highest confidence score that the model returned, you could be missing a great deal of other information with the other tokens (token number 2 - 5). In this scenario, there is another ~57% of information that other token probabilities can be used to calculate a "weighted" Confidence Score, which calibrates the Confidence Score from 1 -> 2.3.<br>
    <img src="https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/AzureLogProbs-TokenProbabilityDistributionExample.png" width="500"/>
</p>

### 3) 95% Confidence Score Interval - Use the distribution of probabilities to calculate a 95% Confidence Interval (range) of plausible answers
   * The previous examples show a single point estimate of the confidence score. This can be misleading as the model may have multiple interpretations of the response.  
   * Azure OpenAI LogProbs can return a probability mass function (PMF) distribution of up to the next 5 tokens including their probabilities.  
   * This calculation uses multiple LogProbs to determine the "confidence interval" of the response.  
   * The confidence interval is calculated by bootstrapping multiple calls (10) to the model (using the same prompt) and calculating the 95% confidence interval of the confidence scores.  
   * The confidence interval can be used to understand the range of possibilities, where 95% of the outcomes will fall within this range as the same question is repeated.
   * Why would you call the model 10x, isn't that overkill? For high-stakes decisions and reasoning (buying a house/car, deciding on a 4-year degree), those extra few calls are well worth the few cents and seconds to get a proper error range.

Example Output:  
![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/ProcessOption-ConfidenceScoreInterval.png)  

## Further Advanced Considerations (Run the ExampleConfidenceIntervalSimulation console project)
This article did not touch on the calibration of the model's confidence score nor the calibration of the model's probability LogProbs.
Because LLMs are essentially neural networks, they can be uncalibrated for specific tasks or domains.
Basically, when the LLM says it is 8/10 confident or probability of 80%, the model should be correct about 80% of the time (within the error rate).  

  * A model that answered 100 questions with a confidence score of 80% should be correct around 80 times. This would be ideal calibration.
    * Note: There is an error rate even if the model is perfectly calibrated around 80%. In the case of 100 questions, 95% of the time we expect the range to be between 72 and 88 correct questions (+/- 8 questions around 80).
  * A model that answered 100 questions with a confidence score of 80% and was only correct 50 times would be extremely overconfident. Note: This is well outside the expected error range.
    * Note: If you simulate this millions of times, the probability of only getting only 50 correct answers if the model claims it is 80% confident is near 0.00%! Not impossible, but if this occurs in real testing the model is clearly uncalibrated and very overconfident.
  * A model that answered 100 questions with a confidence score of 80% and was correct 90 times would be underconfident. Note: This is outside the expected error range.
    * Note: Statistics or a simulation can demonstrate that a model that is 80% confident, but is actually correct 90 times would only occur 0.00233 (0.233%) of the time.

Example Statistical Simulation:  
![Simulation of 80% answers](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/ExampleConfidenceIntervalSimulation/Images/ExampleConfidenceIntervalSimulation-Console.png)  

The topic of calibration is not new and has been studied in decision theory and machine learning.
You can apply both decision intelligence (cognitive science) and machine learning techniques to further calibrate the model performance.
  * Calibrating Chat GPT for Its Overconfidence: https://hubbardresearch.com/chat-gpt-ai-calibration/  
  * Calibrating LLM-Based Evaluator: https://arxiv.org/pdf/2309.13308.pdf  
