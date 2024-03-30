using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;
using System;

namespace AzureOpenAILogProbs
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            {
                Console.Title = "GenAI - Azure OpenAI LogProbs";

                ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
                IConfiguration configuration = configurationBuilder.AddUserSecrets<Program>().Build();

                var azureOpenAIAPIKey = configuration.GetSection("AzureOpenAI")["APIKey"];
                var azureOpenAIEndpoint = configuration.GetSection("AzureOpenAI")["Endpoint"];
                var azureModelDeploymentName = configuration.GetSection("AzureOpenAI")["ModelDeploymentName"];

                Uri azureOpenAIResourceUri = new(azureOpenAIEndpoint!);
                AzureKeyCredential azureOpenAIApiKey = new(azureOpenAIAPIKey!);
                OpenAIClient client = new(azureOpenAIResourceUri, azureOpenAIApiKey);

                var modelDeploymentName = azureModelDeploymentName;

                // From this Wikipedia article from the OpenAI Cookbook

                // https://cookbook.openai.com/examples/using_logprobs
                var wikipediaArticle = """
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

                Console.WriteLine(wikipediaArticle);
                Console.WriteLine(string.Empty);

                Console.WriteLine("Process Questions...");

                var questions = new List<string>
                {
                "When where the Mets founded?", // expected: true
                "Are the Mets a baseball team?", // expected: true
                "Who owns the Mets?", // expected: true
                "Have the Mets won the 2023 World Series?", // expected: false
                "Who are the Boston Red Sox?", // expected: false
                "Where the Mets were a bad team in the 1960s", // expected: true
                "Are the Mets uniform colors only blue and orange?", // expected: ?
                "Are the there only 2 Mets uniform colors?", // expected: ?
                "Do you think the Mets were a historically good team?", // expected: ?
                "Did the Mets have a winning season in their first year of play?", // expected: ?
                "Has Steve Cohen been the longest-serving owner of the Mets?", // expected: ?
                "Is Citi Field located on the exact original site of Shea Stadium?" // expected: ?
                };

                foreach (var question in questions)
                {
                    var promptInstructionsTrueFalse = $"""
                    You retrieved this Wikipedia Article: {wikipediaArticle}. The question is: {question}.
                    Before even answering the question, consider whether you have sufficient information in the Wikipedia article to answer the question fully.
                    Your output should JUST be the boolean true or false, if you have sufficient information in the Wikipedia article to answer the question.
                    Respond with just one word, the boolean true or false. You must output the word 'True', or the word 'False', nothing else.
                    """;

                    var promptInstructionsConfidenceScore = $"""
                    You retrieved this Wikipedia Article: {wikipediaArticle}. The question is: {question}.
                    Before even answering the question, consider whether you have sufficient information in the Wikipedia article to answer the question fully.
                    Your output should JUST be the a single confidence score between 1 to 10, if you have sufficient information in the Wikipedia article to answer the question.
                    Respond with just one confidence score number between 1 to 10. You must output a single number, nothing else.
                    """;

                    var chatCompletionsOptionsTrueFalse = new ChatCompletionsOptions()
                    {
                        DeploymentName = modelDeploymentName, // Use DeploymentName for "model" with non-Azure clients
                        Messages =
                        {
                            // The system message represents instructions or other guidance about how the assistant should behave
                            new ChatRequestSystemMessage("You are a helpful assistant. You will follow the instructions provided in the prompt."),
                            // User messages represent current or historical input from the end user
                            new ChatRequestUserMessage(promptInstructionsTrueFalse)
                        }
                    };

                    var chatCompletionOptionsConfidenceScore = new ChatCompletionsOptions()
                    {
                        DeploymentName = modelDeploymentName, // Use DeploymentName for "model" with non-Azure clients
                        Messages =
                        {
                            // The system message represents instructions or other guidance about how the assistant should behave
                            new ChatRequestSystemMessage("You are a helpful assistant. You will follow the instructions provided in the prompt."),
                            // User messages represent current or historical input from the end user
                            new ChatRequestUserMessage(promptInstructionsConfidenceScore)
                        }
                    };


                    chatCompletionsOptionsTrueFalse.Temperature = 0.0f;
                    chatCompletionsOptionsTrueFalse.EnableLogProbabilities = true;
                    chatCompletionOptionsConfidenceScore.Temperature = 0.0f;
                    chatCompletionOptionsConfidenceScore.EnableLogProbabilities = true;
                    chatCompletionOptionsConfidenceScore.LogProbabilitiesPerToken = 5;

                    Response<ChatCompletions> responseTrueFalse = await client.GetChatCompletionsAsync(chatCompletionsOptionsTrueFalse);
                    Response<ChatCompletions> responseConfidenceScore = await client.GetChatCompletionsAsync(chatCompletionOptionsConfidenceScore);

                    ChatResponseMessage responseMessageTrueFalse = responseTrueFalse.Value.Choices[0].Message;
                    ChatResponseMessage responseMessageConfidenceScore = responseTrueFalse.Value.Choices[0].Message;
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.WriteLine();
                    Console.WriteLine(question);
                    Console.ResetColor();

                    // https://stackoverflow.com/questions/48465737/how-to-convert-log-probability-into-simple-probability-between-0-and-1-values-us
                    var logProbsTrueFalse = responseTrueFalse.Value.Choices[0].LogProbabilityInfo.TokenLogProbabilityResults.Select(a => a.Token + " | Probability: " + Math.Round(Math.Exp(a.LogProbability), 10));
                    var logProbsConfidenceScore = responseConfidenceScore.Value.Choices[0].LogProbabilityInfo.TokenLogProbabilityResults.Select(a => a.Token + " | Probability: " + Math.Round(Math.Exp(a.LogProbability), 10));

                    // write logProbs to console
                    foreach (var logProb in logProbsTrueFalse)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("True/False Answer: " + logProb);
                        Console.ResetColor();
                    }

                    var topLogProbabilityEntriesConfidenceScore = responseConfidenceScore!.Value.Choices[0].LogProbabilityInfo!.TokenLogProbabilityResults!.FirstOrDefault()!.TopLogProbabilityEntries;

                    var confidenceScoreSum = topLogProbabilityEntriesConfidenceScore.Select(a => int.TryParse(a.Token, out _) ? int.Parse(a.Token) * Math.Exp(a.LogProbability) : 0).Sum();
                    Console.WriteLine($"Confidence Score: {Math.Round(confidenceScoreSum, 3)}");

                    Console.WriteLine($"[{responseMessageTrueFalse.Role.ToString().ToUpperInvariant()}]: {responseMessageTrueFalse.Content}");
                    Console.WriteLine($"[{responseMessageConfidenceScore.Role.ToString().ToUpperInvariant()}]: {responseMessageConfidenceScore.Content}");
                }
            }
        }
    }
}
