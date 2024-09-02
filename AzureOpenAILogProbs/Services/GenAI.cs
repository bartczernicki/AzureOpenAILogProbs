using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AzureOpenAILogProbs.DTOs;
using MathNet.Numerics.Random;
using OpenAI;
using OpenAI.Chat;

namespace AzureOpenAILogProbs.Services
{
    internal static class GenAI
    {

        public static string GetPromptInstructions(string sampleWikipediaArticle, Question question, string typeOfResponse)
        {
            // Random seed to prevent KV-Cache from returning the same cached response
            var randomSeed = new Random().Next(0, 1000000);

            var promptInstructionsBase = $"""
                Random Seed: {randomSeed}
                Using this WIKIPEDIA ARTICLE as the ONLY source of information: 
                --START OF WIKIPEDIA ARTICLE--
                {sampleWikipediaArticle}
                -- END OF WIKIPEDIA ARTICLE--
                The question is: 
                -- START OF QUESTION--
                {question.QuestionText}
                -- END OF QUESTION--
                INSTRUCTIONS: 
                Before even answering the question, consider whether you have sufficient information in the Wikipedia article to answer the question fully.
                Do not hallucinate. Do not make up factual information.
                """;

            var promptOutput = string.Empty;
            if (typeOfResponse == "TrueFalse")
            {
                promptOutput = """
                    Your output should JUST be the Boolean true or false, if you have sufficient information in the Wikipedia article to answer the question; you are not answering the actual question.
                    Respond with just one word, the Boolean true or false. You must output the word 'True', or the word 'False', nothing else.
                    """;
            }
            else if (typeOfResponse == "ConfidenceScore")
            {
                promptOutput = $"""
                    Your output should JUST be the a single confidence score between 1 to 10, if you have sufficient information in the Wikipedia article to answer the question; you are not answering the actual question.
                    Respond with just one confidence score number between 1 to 10. You must output a single number, nothing else.
                    """;
            }

            var promptInstructions = $"""
                {promptInstructionsBase}
                {promptOutput}
                """;

            return promptInstructions;
        }

        public static ChatCompletionOptions GetChatCompletionOptions(float temperature, bool includeTopProbabilities)
        {
            var logProbChatCompletionOptions = new ChatCompletionOptions()
            {
                Temperature = temperature,
                IncludeLogProbabilities = true, // turn on log probabilities
                EndUserId = "LogProbsTester",
                TopLogProbabilityCount = includeTopProbabilities ? 5 : 1 // Azure OpenAI maximum is 5
            };

            return logProbChatCompletionOptions;
        }

        public static List<ChatMessage> BuildChatMessageHistory(string promptInstructions)
        {
            var systemChatMessage = new SystemChatMessage("You are an assistant testing large language model features. Follow the instructions provided in the prompt.");
            var userChatMessage = new UserChatMessage(promptInstructions);

            var chatMessages = new List<ChatMessage>();
            chatMessages.Add(systemChatMessage);
            chatMessages.Add(userChatMessage);

            return chatMessages;
        }
    }
}
