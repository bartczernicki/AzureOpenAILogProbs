using Azure;
using Azure.AI.OpenAI;
using AzureOpenAILogProbs.DTOs;
using AzureOpenAILogProbs.Services;
using ConsoleTables;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Configuration;
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

                // Retrieve the Azure OpenAI Configuration Section (secrets.json)
                var azureOpenAISection = configuration.GetSection("AzureOpenAI");
                var azureOpenAIAPIKey = configuration.GetSection("AzureOpenAI")["APIKey"];
                var azureOpenAIEndpoint = configuration.GetSection("AzureOpenAI")["Endpoint"];
                var azureModelDeploymentName = configuration.GetSection("AzureOpenAI")["ModelDeploymentName"];

                // Check if Secrets values are not NULL/missing
                if (string.IsNullOrEmpty(azureOpenAIAPIKey) || string.IsNullOrEmpty(azureOpenAIEndpoint) || string.IsNullOrEmpty(azureModelDeploymentName))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Azure OpenAI Configuration is missing, please check your user secrets configuration.");
                    Console.ReadLine();
                }

                // OpenAI .NET Info: https://github.com/openai/openai-dotnet
                // Define the OpenAI Client Options, increase max retries and delay for the exponential backoff
                // Note: This is better handled by a Polly Retry Policy using 429 status codes for optimization
                var retryPolicy = new ClientRetryPolicy(maxRetries: 10);
                AzureOpenAIClientOptions azureOpenAIClientOptions = new AzureOpenAIClientOptions();
                azureOpenAIClientOptions.RetryPolicy = retryPolicy;

                Uri azureOpenAIResourceUri = new(azureOpenAIEndpoint!);
                AzureKeyCredential azureKeyCredential = new(azureOpenAIAPIKey!);
                
                var client = new AzureOpenAIClient(azureOpenAIResourceUri, azureKeyCredential, azureOpenAIClientOptions);
                var modelDeploymentName = azureModelDeploymentName;
             
                // Define a default Wikipedia Article to use as grounding information for the questions
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

                // PROCESS THE SELECTED OPTION
                if  (
                    (selectedProcessingChoice == (ProcessingOptions.FirstTokenProbability)) ||
                    (selectedProcessingChoice == (ProcessingOptions.FirstTokenProbabilityWithBrierScore))
                    )
                {
                    // Track the list of answers
                    var questionAnswers = new List<QuestionAnswer>();

                    foreach (var question in questions)
                    {
                        var randomSeed = randomGenerator.Next(1, 100000000);

                        var promptInstructionsTrueFalse = Services.GenAI.GetPromptInstructions(sampleWikipediaArticle, question, "TrueFalse");

                        var chatCompletionsOptionsTrueFalse = GenAI.GetChatCompletionOptions(GenAI.OPENAITEMPATURE, false);
                        var chatMessages = GenAI.BuildChatMessageHistory(promptInstructionsTrueFalse);

                        // Get new chat client
                        var chatClient = client.GetChatClient(modelDeploymentName);

                        var response = await chatClient.CompleteChatAsync(chatMessages, chatCompletionsOptionsTrueFalse);
                        var llmResponse = response.Value.Content.FirstOrDefault()!.ToString();

                        // 1) Write the Question to the console
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine();
                        Console.WriteLine($"{question.Number}) Is there information present to answer the question: {question.QuestionText}");
                        Console.ResetColor();

                        // 2) True/False Question - Raw answers to the console
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[Human Expected Answer (Q: Is There Enough Information) - True/False]: {question.EnoughInformationInProvidedContext}");
                        Console.WriteLine($"[LLM {response.Value.Role.ToString().ToUpperInvariant()}  Answer (Q: Is There Enough Information) - True/False]: {llmResponse}");

                        // 3) True/False Question - LLMAnswer Probability Details
                        // More Info: https://stackoverflow.com/questions/48465737/how-to-convert-log-probability-into-simple-probability-between-0-and-1-values-us
                        var probability = Math.Round(Math.Exp(response.Value.ContentTokenLogProbabilities[0].LogProbability), 8);
                        var llmProbsMessage = $"Probability of First Token (Calculated LLM ASSISTANT Probability of Given Answer): {probability}";
                        Console.WriteLine(llmProbsMessage);

                        // 4) Calculate if expected human and LLM answers match
                        // convert responseMessageTrueFalse.Content to bool
                        var llmResponseBool = (bool.Parse(llmResponse));
                        var doesLLMAnswerMatchHumanExpectedAnswer = (llmResponseBool == question.EnoughInformationInProvidedContext);
                        var doAnswersMatchMessage = $"Does the expected human and LLM answer match: {doesLLMAnswerMatchHumanExpectedAnswer}";
                        Console.WriteLine(doAnswersMatchMessage);

                        if (selectedProcessingChoice == ProcessingOptions.FirstTokenProbabilityWithBrierScore)
                        {
                            questionAnswers.Add(new QuestionAnswer
                            {
                                Number = question.Number,
                                LLMAnswer = llmResponseBool,
                                ExpectedAnswer = question.EnoughInformationInProvidedContext,
                                DoesLLMAnswerMatchExpectedAnswer = doesLLMAnswerMatchHumanExpectedAnswer,
                                AnswerProbability = probability
                            });
                        }

                    } // end of foreach question loop
                    
                    // Add Brier Score Information to Console
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
                        Console.WriteLine($"Average Brier Score for sample questions: {Math.Round(questionAnswers.Select(a => a.BrierScore).Average(), 6)}");
                        Console.WriteLine(string.Empty);
                        Console.ResetColor();
                        Console.WriteLine("""
                        Note:
                        Lower Brier Scores are better, closer to 0.0 is ideal. Higher Brier Scores are worse, closer to 1.0 is bad.
                        Average Brier Scores of 0.1 or lower are considered excellent, 0.1 - 0.2 are superior, 0.2 - 0.3 are adequate, 
                        and 0.3-0.35 are acceptable, and finally average Brier scores above 0.35 indicate poor prediction performance.
                        """);
                    }
                }
                else if (selectedProcessingChoice == ProcessingOptions.WeightedProbability)
                {
                    // From this Wikipedia article from the OpenAI Cookbook
                    // https://cookbook.openai.com/examples/using_logprobs

                    foreach (var question in questions)
                    {
                        var promptInstructionsConfidenceScore = Services.GenAI.GetPromptInstructions(sampleWikipediaArticle, question, "ConfidenceScore");

                        var chatMessages = GenAI.BuildChatMessageHistory(promptInstructionsConfidenceScore);

                        // Get new chat client
                        var chatClient = client.GetChatClient(modelDeploymentName);

                        var chatCompletionOptionsConfidenceScore = GenAI.GetChatCompletionOptions(GenAI.OPENAITEMPATURE, true);

                        var response = await chatClient.CompleteChatAsync(chatMessages, chatCompletionOptionsConfidenceScore);
                        var responseValueContent = response.Value.Content[0].Text;


                        //response.Value.

                        // 1) Write the Question to the console
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine();
                        Console.WriteLine($"{question.Number}) Is there information present to answer the question: {question.QuestionText}");
                        Console.ResetColor();

                        // 2) Confidence Score Question - Raw answers to the console
                        var confidenceScore = Int32.Parse(response.Value.Content[0].Text);
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[Human Expected Answer (Q: Is There Enough Information) - True/False]: {question.EnoughInformationInProvidedContext}");
                        Console.WriteLine($"[LLM {response.Value.Role.ToString().ToUpperInvariant()}  Answer (Q: Is There Enough Information) - Confidence Score]: {confidenceScore}");

                        // 3) Confidence Score Question - Process the Confidence Score answer details
                        var logProbsConfidenceScore = response.Value.ContentTokenLogProbabilities.Select(a => a.Token + " | Probability of Self-Confidence Score to answer the question): " + Math.Round(Math.Exp(a.LogProbability), 8));
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
                                var padding = tokenLogProbabilityResult.Token.Length == 2 ? string.Empty : " ";

                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.WriteLine($"\tConfidence Score: {tokenLogProbabilityResult.Token}{padding} | Probability: {Math.Round(Math.Exp(tokenLogProbabilityResult.LogProbability), 8)}");
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
                        Console.WriteLine($"Weighted Probability Calculation Details: Weighted Confidence Score (Sum of Scores*Probabilities): {Math.Round(confidenceScoreSum, 5)}");
                    } // end of foreach question loop
                }
                else if (selectedProcessingChoice == ProcessingOptions.ConfidenceInterval)
                {
                    // On this run, we will use a single question but run it multiple times to calculate the Confidence Interval
                    var question = questions.Where(a => a.Number == 19).FirstOrDefault();

                    // 1) Write the Question to the console
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine();
                    Console.WriteLine(question!.QuestionText);
                    Console.ResetColor();

                    // 2) Set up the prompt instructions and configuration for the Confidence Interval, it will be used for the loop

                    // Loop through the Confidence Score question multiple times (10x)
                    var weightedConfidenceScores = new List<double>();

                    for (int i = 0; i != 10; i++)
                    {
                        var promptInstructionsConfidenceScore = Services.GenAI.GetPromptInstructions(sampleWikipediaArticle, question, "ConfidenceScore");
                        var chatMessages = GenAI.BuildChatMessageHistory(promptInstructionsConfidenceScore);

                        // Get new chat client
                        var chatClient = client.GetChatClient(modelDeploymentName);

                        // Set the Temperature higher to create variance in the responses
                        var chatCompletionOptionsConfidenceScore = GenAI.GetChatCompletionOptions(GenAI.OPENAITEMPATURE, true);

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

                        var topLogProbabilities = response.Value.ContentTokenLogProbabilities[0].TopLogProbabilities;
                        foreach (var tokenLogProbabilityResult in topLogProbabilities)
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
