![Azure OpenAI LogProbs Title](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/AzureLogProbs-Title.png)

## Azure OpenAI Log Probabilities (LogProbs) Examples  
   * .NET Console application that shows examples how Azure OpenAI LogProbs that can be useful for RAG implementations:
     * 1) Calculate First Token Probability - True or False probability, returns the top probability whether the (LLM) model has enough info to answer question  
     * 2) Calculate First Token Probability [With Brier Scores] - True or False probability, returns the top probability whether the (LLM) model has enough info to answer question. Calculates Brier Scores to measure the probabilistic forecasting capability of the model.
     * Weighted Probability of Confidence Score - returns a Self Confidence Score that is weighted from a probability (top 5 probabilities) distribution to give a better (weighted) confidence score estimate to answer a question.
     * Confidence Interval - Calculated from bootstrap simulation of multiple calls to the model. This provides a 95% confidence interval (range) of plausible confidence scores. This is ideal when you need to understand a range of possibilities the model interprets rather than a single point estimate.

![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/AzureLogProbsConsoleApp.png)

## Get Started: Clone the repo. Add this to the User Secrets (Right Click on VS Project -> Manage User Secrets) and run the console application.  
```javascript
{
  "AzureOpenAI": {
    "ModelDeploymentName": "gpt-4-2024-04-09", // Any Azure OpenAI GPT-4  or GPT3.5 model should perform well
    "APIKey": "YOURAZUREOPENAIKEY",
    "Endpoint": "https://YOURAZUREOPENAIENDPOINT.openai.azure.com/"
  }
}
```  

The ability to inspect token log probabilities is turned off by default. To enable this feature, you need to set the EnableLogProbabilities to true. 
For example the .NET Azure OpenAI exposes this as a property.  
```csharp
chatCompletionOptionsConfidenceScore.EnableLogProbabilities = true;
```

## Background Information  

What are LogProbs (Log Probabilities)? Most current LLMs process prompt instructions by predicting the next token and iterate through each token until they reach a stopping point (i.e. max token length, completing the user instructions). 
Each next token that is considered for output is calculated through an internal LLM pipeline that outputs a statistical probability distribution. 
Based on configurations (temperature, top_p etc.) these probabilities can be set and then the LLM selects the next "best token" based on the different configurations. 
Because these LLMs are probabilistic in nature, this is why you may see different tokens output for the same prompt instruction sent to the LLM. 
Below is an example of a question and answer and the associated probabilities for the two tokens (words) that were selected to answer the question: "Who was the first president of the United States?". 
In the example below the model output the answer "George Washington", using the token probabilities of 99.62% and 99.99%.  

![Azure LogProbs Example](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/AzureLogProbs-Example.png)

Recommended Reading on the background of Azure OpenAI LogProbs:  
   * OpenAI Cookbook - LogProbs: https://cookbook.openai.com/examples/using_logprobs  
   * What are LogProbs?: https://www.ignorance.ai/p/what-are-logprobs  

There are several emerging techniques that use multiple calls to a model or several models to arrive at a response, conclusion or a quality decision. Currently, most ways LLMs are used in GenAI production systems is with grounding (RAG) by providing additional contextual information. The model is asked to answer a question, reason over that information etc. However, with poor grounding, this can result in poor results.  

Azure OpenAI LogProbs are a tool that can help and be used to gauge the confidence (probability) of the model's response.
This tremendous capability can empower the AI system to self-correct or guide the user/agent to arrive at a better response.
In the set of examples below, we will simulate a parallel call to the model to gauge the confidence the model has with the presented context and question.
This is illustrated below with the diagram:  

![Azure LogProbs Workflow](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/AzureLogProbs-LogProbsWorkflow.png)  


## Console Processing Options  

### 1) First Token Probability - How Confident is the AI (LLM) Model with the information to answer the question  
   * Uses the Azure OpenAI LogProbs to determine the probability of only the first token in the response.
   * If the probability is high, it is likely the model has enough information (RAG context) to answer the question.  
   * If the probability is low, it is likely the model does not have enough information (RAG context) to answer the question.  
   * The probability can be used as a classification decision threshold of whether the model has enough information (RAG context) to answer the question.     

Example Output:
![Azure OpenAI Log Probs - First Token Prob](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/ProcessOption-FirstTokenProbability.png)  

Note the image above illustrates the True and False output from the LLM as well as the probability of that True or False output.
Because the True or False are the first and only tokens in the response, the first token (LogProb) probability can be used.


### 2) First Token Probability [With Brier Scores] - Calculating Brier Scores of the First Token Probability
   * This example shows how to measure the forecasting & predictive accuracy of the model.
   * Same as the First Token Probability, but also calculates the Brier Score for each of the probability answers.
   * Brier scores (and similar methods in Machine Learning & Statistics) are used to measure the accuracy of probabilistic predictions.
   * The lower the Brier Score, the better the model is at predicting the probability of the answer response.
   * It outputs a table of the Brier Scores for each of the questions and the average Brier Score for all the questions.
   * Averaging Brier scores can tell us a great deal about the overall performance accuracy of the model. Average Brier Scores of 0.1 or lower are considered excellent, 0.1-0.2 are superior, 0.2-0.3 are adequate, and 0.2-0.35 are acceptable, and finally average Brier scores above 0.35 illustrate the overall model performance is poor.

Brier scores will vary depending on the model capabilities, the prompt, and the context of the question. 
By keeping the prompt and context the same, one can compare overall model accuracy performance. 
Note the Brier scores below comparing GPT-4 and GPT-35 (Turbo) models. The GPT-4-20240409 model has a lower Brier score, which means it is more accurate in predicting the probability of the correct answer response. 
In fact, the GPT-4-2024-04-09 correctly arrived at the final answer correctly 11 of the 12 times, whereas the GPT-35 (Turbo) model only matched the expected answer (if there is enough info in the context to answer the question) 10 of 12 times. 
Therefore, the Brier score of the GPT-4-2024-04-09 model is lower, which empirically shows it is more accurate in predicting the probability of estimating it has enough information to answer the provided question.

Example Output:
![Azure OpenAI Log Probs - Calculated Brier Scores - GPT-4](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/AzureLogProbs-CalculatedBrierScores-GPT4-Turbo-20240409.png)  

![Azure OpenAI Log Probs - Calculated Brier Scores - GPT-35](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/AzureLogProbs-CalculatedBrierScores-GPT35-Turbo-0125.png)  


### 3) Weighted Probability of Confidence Score - Model provides a self-confidence score and then assess the probability of the confidence score
   * In the previous examples, only the first token probability was used. Whichever token had the highest probability was used as the True or False determination. 
   * Azure OpenAI LogProbs can return a probability mass function (PMF) distribution of up to the next 5 tokens including their probabilities.  
   * This calculation uses multiple LogProbs to determine the "weighted" probability of the response.  
   * In addition, instead of asking the the model to just provide a True or False determination, the model can provide a confidence score (1-10) of how confident it is in answering the question.
   * The weighted probability is calculated by multiplication: Confidence Score*Probability to give a better weighted estimate of the confidence to answer the question.  
   * The weighted probability can be used as a better calibrated confidence score for the model's response.  

To return multiple Log Probabilities set the LogProbabilitiesPerToken to 5 (current Azure OpenAI maximum, as of this writing):  
```chsarp
chatCompletionOptionsConfidenceScore.Temperature = 0.0f;
chatCompletionOptionsConfidenceScore.EnableLogProbabilities = true;
// For the Confidence Score, we want to investigate 5 of the top log probabilities (PMF)
chatCompletionOptionsConfidenceScore.LogProbabilitiesPerToken = 5;
```  

Example Output:  
![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/ProcessOption-ConfidenceScoreWeightedProbability.png)  

<p align="left" width="100%">
    Below is an example of a Token Probability Distribution when 5 LogProbs tokens are returned with their respective probabilities. In the histogram below, "Confidence Score: 1" has a 42.3% probability; which means the model thinks that is has a very low Confidence Score=1 of answering the question and that low chance is 42.3%. If you just select that highest confidence score that the model returned, you could be missing a great deal of other information with the other tokens (token number 2 - 5). In this scenario, there is another ~57% of information that other token probabilities can be used to calculate a "weighted" Confidence Score, which calibrates the Confidence Score from 1 -> 2.3.<br>
    <img src="https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/AzureLogProbs-TokenProbabilityDistributionExample.png" width="500"/>
</p>

### 4) 95% Confidence Score Interval - Use the distribution of probabilities to calculate a 95% Confidence Interval (range) of plausible answers
   * The previous examples show a single point estimate of the confidence score. This can be misleading as the model may have multiple interpretations of the response.  
   * Azure OpenAI LogProbs can return a probability mass function (PMF) distribution of up to the next 5 tokens including their probabilities.  
   * This calculation uses multiple LogProbs to determine the "confidence interval" of the response.  
   * The confidence interval is calculated by bootstrapping multiple calls (10) to the model (using the same prompt) and calculating the 95% confidence interval of the confidence scores.  
   * The confidence interval can be used to understand the range of possibilities, where 95% of the outcomes will fall within this range as the same question is repeated.
   * Why would you call the model 10x, isn't that overkill? For high-stakes decisions and reasoning (buying a house/car, deciding on a 4-year degree), those extra few calls are well worth the few cents and extra time to get a proper error range.

Example Output:  
![Azure Log Probs](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/ProcessOption-ConfidenceScoreInterval.png)  

## Further Advanced Considerations (Run the ExampleConfidenceIntervalSimulation console project)
This repo did not touch on the calibration of the model's confidence score nor the calibration of the model's probability LogProbs.
Because LLMs are essentially neural networks, they can be uncalibrated for specific tasks or domains.
Basically, when the LLM says it is 8/10 confident or determine a probability of 80%, the model should be correct about 80% of the time (within the error rate).  

  * A model that answered 100 questions with a confidence score of 80% should be correct around 80 times. This would be ideal calibration.
      * Note: There is an error rate even if the model is perfectly calibrated around 80%. In the case of 100 questions, 95% of the time we expect the range to be between 72 and 88 correct questions (+/- 8 questions around the expected average of 80). Why report a 95% Confidence Level and not 100%? Reporting a 100% confidence level makes no sense as the 100% confidence range is from 0 - 100 correct answers. Even though the entire range of probabiliities is infeasable, there is still a very miniscule chance of answering 0 or 100 questions. A 95% confidence level provides a realistic range of plausible results and if you see results outside of this range, something "worth investigating" is potentially happening.
  * A model that answered 100 questions with a confidence score of 80% and was only correct 50 times would be extremely overconfident. This is well outside the expected error range.
    * Note: Statistics or a simulation can demonstrate the probability of only getting only 50 correct answers if the model claims it is 80% confident is near 0.00%! Not impossible, but if this occurs in a production scenario the model is clearly uncalibrated and very overconfident.  
  * A model that answered 100 questions with a confidence score of 80% and was correct 90 times would be underconfident. This is outside the expected error range.  
    * Note: Statistics or a simulation can demonstrate that a model that is 80% confident, but is actually correct more than 90 times would only occur 0.00233 (0.233%) of the time.

Statistical Simulation Showing 10,000,000 simulations and the expected ranges for 100 questions 80% calibration:    
![Simulation of 80% answers](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/ExampleConfidenceIntervalSimulation/Images/ExampleConfidenceIntervalSimulation-Console.png)  

These calibration techniques apply to real-world scenarios. Consider Mainifold Markets (https://manifold.markets/), where human super forecasters wager on the probability of events. 
The collective wisdom of these human super forecasters is highly calibrated in predicting real-world events!  

Example Calibration in a real forecasting environment from Manifold Markets of thousands of forecasts:
![Manifold Markets Calibration](https://raw.githubusercontent.com/bartczernicki/AzureOpenAILogProbs/master/AzureOpenAILogProbs/Images/ManifoldMarketsCalibration.png)  


The topic of calibration is not new and has been studied in decision theory and machine learning.
You can apply both decision intelligence (cognitive science) and machine learning techniques to further calibrate the model performance.
  * Calibrating Chat GPT for Its Overconfidence: https://hubbardresearch.com/chat-gpt-ai-calibration/  
  * Example of forecasters Manifold Markets calibration: https://manifold.markets/calibration  
  * Calibrating a LLM-Based Evaluator: https://arxiv.org/pdf/2309.13308.pdf  
