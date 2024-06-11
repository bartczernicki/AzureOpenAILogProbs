using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Core.Pipeline;
using AzureOpenAILogProbs.DTOs;
using AzureOpenAILogProbs.Services;
using ConsoleTables;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel.Primitives;

namespace AzureOpenAILogProbs
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            {
                Console.Title = "GenAI - Azure OpenAI LogProbs Examples";

                var asciiBanner = """
                       #                                   #######                         #    ### 
                      # #   ###### #    # #####  ######    #     # #####  ###### #    #   # #    #  
                     #   #      #  #    # #    # #         #     # #    # #      ##   #  #   #   #  
                    #     #    #   #    # #    # #####     #     # #    # #####  # #  # #     #  #  
                    #######   #    #    # #####  #         #     # #####  #      #  # # #######  #  
                    #     #  #     #    # #   #  #         #     # #      #      #   ## #     #  #  
                    #     # ######  ####  #    # ######    ####### #      ###### #    # #     # ### 

                    #                     ######                                 #######                                                  
                    #        ####   ####  #     # #####   ####  #####   ####     #       #    #   ##   #    # #####  #      ######  ####  
                    #       #    # #    # #     # #    # #    # #    # #         #        #  #   #  #  ##  ## #    # #      #      #      
                    #       #    # #      ######  #    # #    # #####   ####     #####     ##   #    # # ## # #    # #      #####   ####  
                    #       #    # #  ### #       #####  #    # #    #      #    #         ##   ###### #    # #####  #      #           # 
                    #       #    # #    # #       #   #  #    # #    # #    #    #        #  #  #    # #    # #      #      #      #    # 
                    #######  ####   ####  #       #    #  ####  #####   ####     ####### #    # #    # #    # #      ###### ######  #### 
                    """;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(asciiBanner);
                Console.WriteLine(string.Empty);

                // Azure OpenAI Configuration from user secrets
                ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
                IConfiguration configuration = configurationBuilder.AddUserSecrets<Program>().Build();

                var azureOpenAIAPIKey = configuration.GetSection("AzureOpenAI")["APIKey"];
                var azureOpenAIEndpoint = configuration.GetSection("AzureOpenAI")["Endpoint"];
                var azureModelDeploymentName = configuration.GetSection("AzureOpenAI")["ModelDeploymentName"];

                // Define the OpenAI Client Options, increase max retries and delay for the exponential backoff
                // Note: This is better handled by a Polly Retry Policy using 429 status codes for optimization
                var retryPolicy = new ClientRetryPolicy(maxRetries: 5);
                AzureOpenAIClientOptions azureOpenAIClientOptions = new AzureOpenAIClientOptions();
                azureOpenAIClientOptions.RetryPolicy = retryPolicy;

                Uri azureOpenAIResourceUri = new(azureOpenAIEndpoint!);
                AzureKeyCredential azureKeyCredential = new(azureOpenAIAPIKey!);

                // Info: https://github.com/Azure/azure-sdk-for-net/tree/Azure.AI.OpenAI_1.0.0-beta.16/sdk/openai/Azure.AI.OpenAI 
                var client = new AzureOpenAIClient(azureOpenAIResourceUri, azureKeyCredential, azureOpenAIClientOptions);
                var modelDeploymentName = azureModelDeploymentName;
             
                // Define a sample Wikipedia Article to use as grounding information for the questions
                var sampleWikipediaArticle = Services.Questions.GetContextForQuestions();

                // User input selection - Processing Options
                ProcessingOptions selectedProcessingChoice = (ProcessingOptions) 0;
                bool validInput = false;
                while (!validInput)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(string.Empty); 
                    Console.WriteLine("Select one of the options to run, by typing either 1 through 3:");
                    Console.WriteLine("1) First Token Probability - True or False, whether the model has enough info to answer question.");
                    Console.WriteLine("2) First Token Probability - True or False, whether the model has enough info to answer question [With Brier Scores].");
                    Console.WriteLine("3) Weighted Probability of Confidence Score - Self Confidence Score that is weighted from LogProbs PMF distribution.");
                    Console.WriteLine("4) Confidence Interval - Calculated from bootstrap simulation of multiple calls to the model.");

                    var insertedText = Console.ReadLine();
                    string trimmedInput = insertedText!.Trim();

                    if (trimmedInput == "1" || trimmedInput == "2" || trimmedInput == "3" || trimmedInput == "4")
                    {
                        validInput = true;
                        selectedProcessingChoice = (ProcessingOptions)Int32.Parse(trimmedInput);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Incorrect selection!!!!");
                    }
                }
                Console.WriteLine("\r\nYou selected: {0}\r\n", selectedProcessingChoice);

                Console.ForegroundColor= ConsoleColor.Magenta;
                Console.WriteLine("Using the following Wikipedia Article as grounding information for questions...");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"{sampleWikipediaArticle}\r\n");

                // Define sample list of questions to ask the model
                // Note: The questions are a mix of True/False questions and highly dependent on the sample Wikipedia article above (Mets)
                var questions = Services.Questions.GetQuestions();

                // Process the selected option
                if  (
                    (selectedProcessingChoice == (ProcessingOptions.FirstTokenProbability)) ||
                    (selectedProcessingChoice == (ProcessingOptions.FirstTokenProbabilityWithBrierScore))
                    )
                {
                    // Track the list of answers
                    var questionAnswers = new List<QuestionAnswer>();

                    foreach (var question in questions)
                    {
                        var promptInstructionsTrueFalse = $"""
                        Using this Wikipedia Article as the ONLY source of information: 
                        --START OF WIKIPEDIA ARTICLE--
                        {sampleWikipediaArticle}
                        -- END OF WIKIPEDIA ARTICLE--
                        The question is: 
                        -- START OF QUESTION--
                        {question.QuestionText}
                        -- END OF QUESTION--
                        INSTRUCTIONS: 
                        Before even answering the question, consider whether you have sufficient information in the Wikipedia article to answer the question fully.
                        Your output should JUST be the Boolean true or false, if you have sufficient information in the Wikipedia article to answer the question.
                        Respond with just one word, the Boolean true or false. You must output the word 'True', or the word 'False', nothing else.
                        """;


                        var chatCompletionsOptionsTrueFalse = GenAI.GetChatCompletionOptions(0.0f, false);

                        //var systemChatMessage = new SystemChatMessage("You are an assistant testing large language model features. Follow the instructions provided in the prompt.");
                        //var userChatMessage = new UserChatMessage(promptInstructionsTrueFalse);

                        //var chatMessages = new List<ChatMessage>();
                        //chatMessages.Add(systemChatMessage);
                        //chatMessages.Add(userChatMessage);

                        var chatMessages = GenAI.BuildChatMessageHistory(promptInstructionsTrueFalse);

                        // Get new chat client
                        var chatClient = client.GetChatClient(modelDeploymentName);

                        var response = await chatClient.CompleteChatAsync(chatMessages, chatCompletionsOptionsTrueFalse);
                        var responseValueContent = response.Value.Content.ToString();

                        // 1) Write the Question to the console
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine();
                        Console.WriteLine($"{question.Number}) {question.QuestionText}");
                        Console.ResetColor();

                        // 2) True/False Question - Raw answers to the console
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[Human Expected LLMAnswer (Enough Information) - True/False]: {question.EnoughInformationInProvidedContext}");
                        Console.WriteLine($"[LLM {response.Value.Role.ToString().ToUpperInvariant()} (Enough Information) - True/False]: {responseValueContent}");

                        
                        // 3) True/False Question - LLMAnswer Details
                        // https://stackoverflow.com/questions/48465737/how-to-convert-log-probability-into-simple-probability-between-0-and-1-values-us
                        var logProbsTrueFalse = response.Value.ContentTokenLogProbabilities.Select(a => a.Token + " | Probability of First Token (LLM Probability of having enough info for question): " + Math.Round(Math.Exp(a.LogProbability), 8));
                        var probability = Math.Round(Math.Exp(response.Value.ContentTokenLogProbabilities[0].LogProbability), 8);
                        // Write out the first token probability
                        foreach (var logProb in logProbsTrueFalse)
                        {
                            Console.WriteLine($"True/False LLMAnswer: {logProb}");
                        }

                        // convert responseMessageTrueFalse.Content to bool
                        var responseTrueFalseBool = responseValueContent == "True";
                        if (selectedProcessingChoice == ProcessingOptions.FirstTokenProbabilityWithBrierScore)
                        {
                            var responseText = response.Value.Content[0].Text;
                            questionAnswers.Add(new QuestionAnswer
                            {
                                Number = question.Number,
                                LLMAnswer = bool.Parse(responseText),
                                ExpectedAnswer = question.EnoughInformationInProvidedContext,
                                DoesLLMAnswerMatchExpectedAnswer = (bool.Parse(responseText) == question.EnoughInformationInProvidedContext),
                                AnswerProbability = probability
                            });
                        }
                    } // end of foreach question loop
                    
                    // Add Brier Score Information
                    if (selectedProcessingChoice == ProcessingOptions.FirstTokenProbabilityWithBrierScore)
                    {
                        // Show the Brier Score for the answers in a table
                        Console.ForegroundColor = ConsoleColor.Green;
                        var consoleTable = ConsoleTable.From<QuestionAnswer>(questionAnswers);
                        consoleTable.Options.EnableCount = false;
                        Console.WriteLine();
                        Console.WriteLine($"| CALCULATED BRIER SCORES FOR: {modelDeploymentName!.ToUpper()}");
                        Console.WriteLine($"|-----------------------------");
                        consoleTable.Write(Format.Minimal);
                        Console.WriteLine($" Average Brier Score for sample questions: {Math.Round(questionAnswers.Select(a => a.BrierScore).Average(), 6)}");
                        Console.ResetColor();
                    }
                }
                else if (selectedProcessingChoice == ProcessingOptions.WeightedProbability)
                {
                    // From this Wikipedia article from the OpenAI Cookbook
                    // https://cookbook.openai.com/examples/using_logprobs

                    foreach (var question in questions)
                    {
                        var promptInstructionsConfidenceScore = $"""
                        Using this Wikipedia Article as the ONLY source of information: 
                        --START OF WIKIPEDIA ARTICLE--
                        {sampleWikipediaArticle}
                        -- END OF WIKIPEDIA ARTICLE--
                        The question is: 
                        -- START OF QUESTION--
                        {question.QuestionText}
                        -- END OF QUESTION--
                        INSTRUCTIONS: Before even answering the question, consider whether you have sufficient information in the Wikipedia article to answer the question fully.
                        Your output should JUST be the a single confidence score between 1 to 10, if you have sufficient information in the Wikipedia article to answer the question.
                        Respond with just one confidence score number between 1 to 10. You must output a single number, nothing else.
                        """;

                        var chatMessages = GenAI.BuildChatMessageHistory(promptInstructionsConfidenceScore);

                        // Get new chat client
                        var chatClient = client.GetChatClient(modelDeploymentName);

                        var chatCompletionOptionsConfidenceScore = GenAI.GetChatCompletionOptions(0.0f, true);

                        var response = await chatClient.CompleteChatAsync(chatMessages, chatCompletionOptionsConfidenceScore);
                        var responseValueContent = response.Value.Content[0].Text;


                        //response.Value.

                        // 1) Write the Question to the console
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine();
                        Console.WriteLine($"{question.Number}) {question.QuestionText}");
                        Console.ResetColor();

                        // 2) Confidence Score Question - Raw answers to the console
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[Human Expected LLMAnswer (Enough Information) - True/False]: {question.EnoughInformationInProvidedContext}");
                        Console.WriteLine($"[LLM {response.Value.Role.ToString().ToUpperInvariant()} (Enough Information) - Confidence Score]: {response.Value.Content[0].Text}");

                        // 3) Confidence Score Question - Process the Confidence Score answer details
                        var logProbsConfidenceScore = response.Value.ContentTokenLogProbabilities.Select(a => a.Token + " | Probability of First Token (LLM Probability of Self-Confidence Score in having enough info for question): " + Math.Round(Math.Exp(a.LogProbability), 8));
                        // Write out the first token probability
                        foreach (var logProb in logProbsConfidenceScore)
                        {
                            Console.WriteLine($"Weighted Probability Calculation Details: {logProb}");
                        }
                        // Write out up to the first 5 valid tokens (integers that match prompt instructions)
                        Console.WriteLine("\tProbability Distribution (PMF) for the Confidence Score (Top 5 LogProbs tokens):");
                        foreach (var tokenLogProbabilityResult in response.Value.ContentTokenLogProbabilities[0].TopLogProbabilities)
                        {
                            if (int.TryParse(tokenLogProbabilityResult.Token, out _))
                            {
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.WriteLine($"\tConfidence Score: {tokenLogProbabilityResult.Token} | Probability: {Math.Round(Math.Exp(tokenLogProbabilityResult.LogProbability), 8)}");
                                Console.ForegroundColor = ConsoleColor.Cyan;
                            }

                        }

                        // 4) Retrieve the Top 5 Log Probability Entries for the Confidence Score
                        var topLogProbabilityEntriesConfidenceScore = response.Value.ContentTokenLogProbabilities[0].TopLogProbabilities;

                        // 5) Calculate the PMF (Probability Mass Function) for the Confidence Score, for only the valid integer tokens
                        var confidenceScoreProbabilityMassFunctionSum = topLogProbabilityEntriesConfidenceScore.Select(a => int.TryParse(a.Token, out _) ? Math.Exp(a.LogProbability) : 0).Sum();
                        var pmfScaleFactor = 1 / confidenceScoreProbabilityMassFunctionSum;

                        // 6) Calculate the Weighted (Sum of all the Score*Probability) Confidence Score
                        var confidenceScoreSum = topLogProbabilityEntriesConfidenceScore.Select(a => int.TryParse(a.Token, out _) ? int.Parse(a.Token) * Math.Exp(a.LogProbability) * pmfScaleFactor : 0).Sum();

                        Console.WriteLine($"Weighted Probability Calculation Details: Valid Tokens Probability Mass Function: {Math.Round(confidenceScoreProbabilityMassFunctionSum, 5)}");
                        Console.WriteLine($"Weighted Probability Calculation Details: Scale Factor for PMF: {pmfScaleFactor}");
                        Console.WriteLine($"Weighted Probability Calculation Details: Weighted (Sum of Scores*Probabilities) Confidence Score: {Math.Round(confidenceScoreSum, 5)}");
                    } // end of foreach question loop
                }
                else if (selectedProcessingChoice == ProcessingOptions.ConfidenceInterval)
                {
                    // On this run, we will use a single question but run it multiple times to calculate the Confidence Interval
                    var question = "Has Steve Cohen been the longest-serving owner of the Mets?";

                    // 1) Write the Question to the console
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine();
                    Console.WriteLine(question);
                    Console.ResetColor();

                    // 2) Set up the prompt instructions and configuration for the Confidence Interval, it will be used for the loop
                    var promptInstructionsConfidenceScore = $"""
                        Using this Wikipedia Article as the ONLY source of information: 
                        --START OF WIKIPEDIA ARTICLE--
                        {sampleWikipediaArticle}
                        -- END OF WIKIPEDIA ARTICLE--
                        The question is: 
                        -- START OF QUESTION--
                        {question}
                        -- END OF QUESTION--
                        INSTRUCTIONS: 
                        Before even answering the question, consider whether you have sufficient information in the Wikipedia article to answer the question fully.
                        Your output should JUST be the a single confidence score between 1 to 10, if you have sufficient information in the Wikipedia article to answer the question.
                        Respond with just one confidence score number between 1 to 10. You must output a single number, nothing else.
                        """;


                    var chatMessages = GenAI.BuildChatMessageHistory(promptInstructionsConfidenceScore);

                    // Get new chat client
                    var chatClient = client.GetChatClient(modelDeploymentName);
                    
                    // Set the Temperature higher to create variance in the responses
                    var chatCompletionOptionsConfidenceScore = GenAI.GetChatCompletionOptions(0.8f, true);

                    // Loop through the Confidence Score question multiple times (10x)
                    var weightedConfidenceScores = new List<double>();
                    for (int i = 0; i != 10; i++)
                    {
                        var response = await chatClient.CompleteChatAsync(chatMessages, chatCompletionOptionsConfidenceScore);
                        var responseValueContent = response.Value.Content[0].Text;
                        Console.ForegroundColor = ConsoleColor.Yellow;

                        // 2) Confidence Score Question - Raw answers to the console
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[{response.Value.Role.ToString().ToUpperInvariant()} - Confidence Score]: {responseValueContent}");

                        // 3) Confidence Score Question - Process the Confidence Score answer details
                        var logProbsConfidenceScore = response.Value.ContentTokenLogProbabilities.Select(a => $"Confidence Score: {a.Token} | Probability of First Token: {Math.Round(Math.Exp(a.LogProbability), 10)}");

                        // Write out the first token probability
                        foreach (var logProb in logProbsConfidenceScore)
                        {
                            Console.WriteLine($"Weighted Probability Calculation Details: {logProb}");
                        }
                        // Write out up to the first 5 valid tokens (integers that match prompt instructions)
                        Console.WriteLine("\tProbability Distribution (PMF) for the Confidence Score (Top 5 LogProbs tokens):");
                        foreach (var tokenLogProbabilityResult in response.Value.ContentTokenLogProbabilities[0].TopLogProbabilities)
                        {
                            if (int.TryParse(tokenLogProbabilityResult.Token, out _))
                            {
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.WriteLine($"\tConfidence Score: {tokenLogProbabilityResult.Token} | Probability: {Math.Round(Math.Exp(tokenLogProbabilityResult.LogProbability), 8)}");
                                Console.ForegroundColor = ConsoleColor.Cyan;
                            }

                        }

                        // 4) Retrieve the Top 5 Log Probability Entries for the Confidence Score
                        var topLogProbabilityEntriesConfidenceScore = response.Value.ContentTokenLogProbabilities[0].TopLogProbabilities;

                        // 5) Calculate the PMF (Probability Mass Function) for the Confidence Score, for only the valid integer tokens
                        var confidenceScoreProbabilityMassFunctionSum = topLogProbabilityEntriesConfidenceScore.Select(a => int.TryParse(a.Token, out _) ? Math.Exp(a.LogProbability) : 0).Sum();
                        var pmfScaleFactor = 1 / confidenceScoreProbabilityMassFunctionSum;

                        // 6) Calculate the Weighted (Sum of all the Score*Probability) Confidence Score
                        var confidenceScoreSum = topLogProbabilityEntriesConfidenceScore.Select(a => int.TryParse(a.Token, out _) ? int.Parse(a.Token) * Math.Exp(a.LogProbability) * pmfScaleFactor : 0).Sum();
                        weightedConfidenceScores.Add(confidenceScoreSum);

                        Console.WriteLine($"Weighted Probability Calculation Details: Valid Tokens Probability Mass Function: {Math.Round(confidenceScoreProbabilityMassFunctionSum, 5)}");
                        Console.WriteLine($"Weighted Probability Calculation Details: Scale Factor for PMF: {pmfScaleFactor}");
                        Console.WriteLine($"Weighted Probability Calculation Details: Weighted (sum of Scores*Probabilities) Confidence Score: {Math.Round(confidenceScoreSum, 5)}");
                        Console.WriteLine();
                    } // end of for loop

                    // Write a bootstrap simulation of the average of weightedConfidenceScores
                    // Sample with replacement from the weightedConfidenceScores
                    var random = new Random();  // Add seed for reproducibility
                    var bootstrapConfidenceScores = new List<double>();
                    for (int i = 0; i != 1000; i++) // 1,000 Bootstrap Simulations (bootstrap estimates)
                    {
                        var bootstrapSample = new List<double>();
                        for (int j = 0; j != 10; j++) // Sample size should match the number of times the question was asked
                        {
                            var randomIndex = random.Next(0, weightedConfidenceScores.Count);
                            bootstrapSample.Add(weightedConfidenceScores[randomIndex]);
                        }
                        bootstrapConfidenceScores.Add(bootstrapSample.Average());
                    }

                    // DEBUG write out weightedConfidenceScores to a comma-separated string
                    //var bootstrapConfidenceScoresString = string.Join(",", weightedConfidenceScores);
                    //Console.WriteLine(bootstrapConfidenceScoresString);

                    // Calculate Standard Deviation of weightedConfidenceScores
                    var bootstrapConfidenceScoresStandardDeviation = Math.Round(bootstrapConfidenceScores.StandardDeviation(), 3);


                    // Calculate the 95% Confidence Interval
                    var bootstrapConfidenceScoresSorted = bootstrapConfidenceScores.OrderBy(a => a).ToList();
                    // Calculate the min and the max
                    var minimumScore = Math.Round(bootstrapConfidenceScoresSorted.Min(), 3);
                    var maximumScore = Math.Round(bootstrapConfidenceScoresSorted.Max(), 3);
                    // Calculate the 2.5% and 97.5% percentiles
                    var lowerPercentile = Math.Round(bootstrapConfidenceScoresSorted[25], 3);
                    var upperPercentile = Math.Round(bootstrapConfidenceScoresSorted[975], 3);
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Weighted Probability Calculation Details: Minimum & Maximum Range: {minimumScore} - {maximumScore}");
                    Console.WriteLine($"Weighted Probability Calculation Details: 95% Confidence Score Interval: {lowerPercentile} - {upperPercentile}");
                    Console.WriteLine($"Weighted Probability Calculation Details: Bootstrap Standard Error (Standard Deviation): {bootstrapConfidenceScoresStandardDeviation}");
                    Console.ResetColor();
                }
            }
        }
    }
}
