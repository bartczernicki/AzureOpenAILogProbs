using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;
using System;
using Azure.Core;
using MathNet.Numerics.Statistics;

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
                OpenAIClientOptions openAIClientOptions = new OpenAIClientOptions { Retry = { Delay = TimeSpan.FromSeconds(2), MaxDelay = TimeSpan.FromSeconds(30), MaxRetries = 5, Mode = RetryMode.Exponential } };
                Uri azureOpenAIResourceUri = new(azureOpenAIEndpoint!);
                AzureKeyCredential azureOpenAIApiKey = new(azureOpenAIAPIKey!);
                OpenAIClient client = new(azureOpenAIResourceUri, azureOpenAIApiKey, openAIClientOptions);

                var modelDeploymentName = azureModelDeploymentName;

                // Define a sample Wikipedia Article to use as grounding information for the questions
                // https://en.wikipedia.org/wiki/New_York_Mets 
                var sampleWikipediaArticle = """
                The New York Mets are an American professional baseball team based in the New York City borough of Queens.
                The Mets compete in Major League Baseball (MLB) as a member of the National League (NL) East Division.
                They are one of two major league clubs based in New York City, the other being the American League's (AL) New York Yankees.
                One of baseball's first expansion teams, the Mets were founded in 1962 to replace New York's departed NL teams, the Brooklyn Dodgers and the New York Giants.
                The team's colors evoke the blue of the Dodgers and the orange of the Giants.
                For the 1962 and 1963 seasons, the Mets played home games at the Polo Grounds in Manhattan before moving to Queens.
                From 1964 to 2008, the Mets played their home games at Shea Stadium, named after William Shea, the founder of the Continental League, a proposed third major league, the announcement of which prompted their admission as an NL expansion team.
                Since 2009, the Mets have played their home games at Citi Field next to the site where Shea Stadium once stood.
                In their inaugural season, the Mets posted a record of 40–120, the worst regular-season record since MLB went to a 162-game schedule.
                The team never finished better than second-to-last in the 1960s until the "Miracle Mets" beat the Baltimore Orioles in the 1969 World Series, considered one of the biggest upsets in World Series history despite the Mets having won 100 games that season.
                The Mets have qualified for the postseason ten times, winning the World Series twice (1969 and 1986) and winning five National League pennants (most recently in 2000 and 2015), and six National League East division titles.
                Since 2020, the Mets have been owned by billionaire hedge fund manager Steve Cohen, who purchased the team for $2.4 billion.[10] As of 2023, Forbes ranked the Mets as the sixth most valuable MLB team, valued at $2.9 billion.
                As of the end of the 2023 regular season, the team's overall win–loss record is 4,727–5,075–8 (.482).
                """;

                // User input selection - Processing Options
                ProcessingOptions selectedProcessingChoice = (ProcessingOptions) 0;
                bool validInput = false;
                while (!validInput)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(string.Empty); 
                    Console.WriteLine("Select one of the options to run, by typing either 1 through 3:");
                    Console.WriteLine("1) First Token Probability - True or False, whether the model has enough info to answer question.");
                    Console.WriteLine("2) Weighted Probability of Confidence Score - Self Confidence Score that is weighted from LogProbs PMF distribution.");
                    Console.WriteLine("3) Confidence Interval - Calculated from bootstrap simulation of multiple calls to the model.");

                    var insertedText = Console.ReadLine();
                    string trimmedInput = insertedText!.Trim();

                    if (trimmedInput == "1" || trimmedInput == "2" || trimmedInput == "3")
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

                // DEfine sample list of questions to ask the model
                var questions = new List<string>
                {
                "When where the Mets founded?", // expected: true
                "Are the Mets a baseball team?", // expected: true
                "Who owns the Mets?", // expected: true
                "Have the Mets won the 2023 World Series?", // expected: false
                "Who are the Boston Red Sox?", // expected: false
                "Where the Mets were a bad team in the 1960s?", // expected: true
                "Are the Mets uniform colors only blue and orange?", // expected: ?
                "Are the there only 2 Mets uniform colors?", // expected: ?
                "Do you think the Mets were a historically good team?", // expected: ?
                "Did the Mets have a winning season in their first year of play?", // expected: ?
                "Has Steve Cohen been the longest-serving owner of the Mets?", // expected: ?
                "Is Citi Field located on the exact original site of Shea Stadium?" // expected: ?
                };

                // Process the selected option
                if (selectedProcessingChoice == (ProcessingOptions.FirstTokenProbability))
                {
                    foreach (var question in questions)
                    {
                        var promptInstructionsTrueFalse = $"""
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
                        Your output should JUST be the boolean true or false, if you have sufficient information in the Wikipedia article to answer the question.
                        Respond with just one word, the boolean true or false. You must output the word 'True', or the word 'False', nothing else.
                        """;

                        var chatCompletionsOptionsTrueFalse = new ChatCompletionsOptions()
                        {
                            DeploymentName = modelDeploymentName, // Use DeploymentName for "model" with Azure clients
                            Messages =
                            {
                                // The system message represents instructions or other guidance about how the assistant should behave
                                new ChatRequestSystemMessage("You are an assistant testing large language model features. Follow the instructions provided in the prompt."),
                                // User messages represent current or historical input from the end user
                                new ChatRequestUserMessage(promptInstructionsTrueFalse)
                            }
                        };

                        chatCompletionsOptionsTrueFalse.Temperature = 0.0f;
                        chatCompletionsOptionsTrueFalse.EnableLogProbabilities = true;

                        Response<ChatCompletions> responseTrueFalse = await client.GetChatCompletionsAsync(chatCompletionsOptionsTrueFalse);
                        ChatResponseMessage responseMessageTrueFalse = responseTrueFalse.Value.Choices[0].Message;

                        // 1) Write the Question to the console
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine();
                        Console.WriteLine(question);
                        Console.ResetColor();

                        // 2) True/False Question - Raw answers to the console
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[{responseMessageTrueFalse.Role.ToString().ToUpperInvariant()} - True/False]: {responseMessageTrueFalse.Content}");

                        // 3) True/False Question - Answer Details
                        // https://stackoverflow.com/questions/48465737/how-to-convert-log-probability-into-simple-probability-between-0-and-1-values-us
                        var logProbsTrueFalse = responseTrueFalse.Value.Choices[0].LogProbabilityInfo.TokenLogProbabilityResults.Select(a => a.Token + " | Probability of First Token (LLM Probability of having enough info for question): " + Math.Round(Math.Exp(a.LogProbability), 8));
                        // Write out the first token probability
                        foreach (var logProb in logProbsTrueFalse)
                        {
                            Console.WriteLine($"True/False Answer: {logProb}");
                        }
                    } // end of foreach question loop
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
                        {question}
                        -- END OF QUESTION--
                        INSTRUCTIONS: Before even answering the question, consider whether you have sufficient information in the Wikipedia article to answer the question fully.
                        Your output should JUST be the a single confidence score between 1 to 10, if you have sufficient information in the Wikipedia article to answer the question.
                        Respond with just one confidence score number between 1 to 10. You must output a single number, nothing else.
                        """;

                        var chatCompletionOptionsConfidenceScore = new ChatCompletionsOptions()
                        {
                            DeploymentName = modelDeploymentName, // Use DeploymentName for "model" with Azure clients
                            Messages =
                            {
                                // The system message represents instructions or other guidance about how the assistant should behave
                                new ChatRequestSystemMessage("You are an assistant testing large language model features. Follow the instructions provided in the prompt."),
                                // User messages represent current or historical input from the end user
                                new ChatRequestUserMessage(promptInstructionsConfidenceScore)
                            }
                        };

                        chatCompletionOptionsConfidenceScore.Temperature = 0.0f;
                        chatCompletionOptionsConfidenceScore.EnableLogProbabilities = true;
                        // For the Confidence Score, we want to see 5 of the top log probabilities (PMF); 5 is currently the max
                        chatCompletionOptionsConfidenceScore.LogProbabilitiesPerToken = 5;

                        
                        Response<ChatCompletions> responseConfidenceScore = await client.GetChatCompletionsAsync(chatCompletionOptionsConfidenceScore);
                        ChatResponseMessage responseMessageConfidenceScore = responseConfidenceScore.Value.Choices[0].Message;

                        // 1) Write the Question to the console
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine();
                        Console.WriteLine(question);
                        Console.ResetColor();

                        // 2) Confidence Score Question - Raw answers to the console
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[{responseMessageConfidenceScore.Role.ToString().ToUpperInvariant()} - Confidence Score]: {responseMessageConfidenceScore.Content}");

                        // 3) Confidence Score Question - Process the Confidence Score answer details
                        var logProbsConfidenceScore = responseConfidenceScore.Value.Choices[0].LogProbabilityInfo.TokenLogProbabilityResults.Select(a => a.Token + " | Probability of First Token (LLM Probability of Self-Confidence Score in having enough info for question): " + Math.Round(Math.Exp(a.LogProbability), 8));
                        // Write out the first token probability
                        foreach (var logProb in logProbsConfidenceScore)
                        {
                            Console.WriteLine($"Weighted Probability Calculation Details: {logProb}");
                        }
                        // Write out up to the first 5 valid tokens (integers that match prompt instructions)
                        Console.WriteLine("\tProbability Distribution (PMF) for the Confidence Score (Top 5 LogProbs tokens):");
                        foreach (var tokenLogProbabilityResult in responseConfidenceScore!.Value.Choices[0].LogProbabilityInfo!.TokenLogProbabilityResults!.FirstOrDefault()!.TopLogProbabilityEntries)
                        {
                            if (int.TryParse(tokenLogProbabilityResult.Token, out _))
                            {
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.WriteLine($"\tConfidence Score: {tokenLogProbabilityResult.Token} | Probability: {Math.Round(Math.Exp(tokenLogProbabilityResult.LogProbability), 8)}");
                                Console.ForegroundColor = ConsoleColor.Cyan;
                            }

                        }

                        // 4) Retrieve the Top 5 Log Probability Entries for the Confidence Score
                        var topLogProbabilityEntriesConfidenceScore = responseConfidenceScore!.Value.Choices[0].LogProbabilityInfo!.TokenLogProbabilityResults!.FirstOrDefault()!.TopLogProbabilityEntries;

                        // 5) Calculate the PMF (Probability Mass Function) for the Confidence Score, for only the valid integer tokens
                        var confidenceScoreProbabilityMassFunctionSum = topLogProbabilityEntriesConfidenceScore.Select(a => int.TryParse(a.Token, out _) ? Math.Exp(a.LogProbability) : 0).Sum();
                        var pmfScaleFactor = 1 / confidenceScoreProbabilityMassFunctionSum;

                        // 6) Calculate the Weighted (Sum of all the Score*Probability) Confidence Score
                        var confidenceScoreSum = topLogProbabilityEntriesConfidenceScore.Select(a => int.TryParse(a.Token, out _) ? int.Parse(a.Token) * Math.Exp(a.LogProbability) * pmfScaleFactor : 0).Sum();

                        Console.WriteLine($"Weighted Probability Calculation Details: Valid Tokens Probability Mass Function: {Math.Round(confidenceScoreProbabilityMassFunctionSum, 5)}");
                        Console.WriteLine($"Weighted Probability Calculation Details: Scale Factor for PMF: {pmfScaleFactor}");
                        Console.WriteLine($"Weighted Probability Calculation Details: Weighted (sum of Scores*Probabilities) Confidence Score: {Math.Round(confidenceScoreSum, 5)}");
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

                    var chatCompletionOptionsConfidenceScore = new ChatCompletionsOptions()
                    {
                        DeploymentName = modelDeploymentName, // Use DeploymentName for "model" with Azure clients
                        Messages =
                            {
                                // The system message represents instructions or other guidance about how the assistant should behave
                                new ChatRequestSystemMessage("You are an assistant testing large language model features. Follow the instructions provided in the prompt."),
                                // User messages represent current or historical input from the end user
                                new ChatRequestUserMessage(promptInstructionsConfidenceScore)
                            }
                    };

                    chatCompletionOptionsConfidenceScore.Temperature = 0.0f;
                    chatCompletionOptionsConfidenceScore.EnableLogProbabilities = true;
                    // For the Confidence Score, we want to see 5 of the top log probabilities (PMF)
                    chatCompletionOptionsConfidenceScore.LogProbabilitiesPerToken = 5;

                    // Loop through the Confidence Score question multiple times (10x)
                    var weightedConfidenceScores = new List<double>();
                    for (int i =0; i != 10; i++)
                    {
                        Response<ChatCompletions> responseConfidenceScore = await client.GetChatCompletionsAsync(chatCompletionOptionsConfidenceScore);
                        ChatResponseMessage responseMessageConfidenceScore = responseConfidenceScore.Value.Choices[0].Message;
                        Console.ForegroundColor = ConsoleColor.Yellow;

                        // 2) Confidence Score Question - Raw answers to the console
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[{responseMessageConfidenceScore.Role.ToString().ToUpperInvariant()} - Confidence Score]: {responseMessageConfidenceScore.Content}");

                        // 3) Confidence Score Question - Process the Confidence Score answer details
                        var logProbsConfidenceScore = responseConfidenceScore.Value.Choices[0].LogProbabilityInfo.TokenLogProbabilityResults.Select(a => $"Confidence Score: {a.Token} | Probability of First Token: {Math.Round(Math.Exp(a.LogProbability), 10)}");
                        
                        // Write out the first token probability
                        foreach (var logProb in logProbsConfidenceScore)
                        {
                            Console.WriteLine($"Weighted Probability Calculation Details: {logProb}");
                        }
                        // Write out up to the first 5 valid tokens (integers that match prompt instructions)
                        Console.WriteLine("\tProbability Distribution (PMF) for the Confidence Score (Top 5 LogProbs tokens):");
                        foreach (var tokenLogProbabilityResult in responseConfidenceScore!.Value.Choices[0].LogProbabilityInfo!.TokenLogProbabilityResults!.FirstOrDefault()!.TopLogProbabilityEntries)
                        {
                            if (int.TryParse(tokenLogProbabilityResult.Token, out _))
                            {
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.WriteLine($"\tConfidence Score: {tokenLogProbabilityResult.Token} | Probability: {Math.Round(Math.Exp(tokenLogProbabilityResult.LogProbability), 8)}");
                                Console.ForegroundColor = ConsoleColor.Cyan;
                            }

                        }

                        // 4) Retrieve the Top 5 Log Probability Entries for the Confidence Score
                        var topLogProbabilityEntriesConfidenceScore = responseConfidenceScore!.Value.Choices[0].LogProbabilityInfo!.TokenLogProbabilityResults!.FirstOrDefault()!.TopLogProbabilityEntries;

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
                        for (int j = 0; j != 10; j++) // Keep the sample size at least 30, could be higher to reduce variance/error
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
