using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;

namespace AzureOpenAILogProbs.Services
{
    internal static class GenAI
    {
        public static ChatCompletionOptions GetChatCompletionOptions(float temperature, bool includeTopProbabilities)
        {
            var logProbChatCompletionOptions = new ChatCompletionOptions()
            {
                Temperature = temperature,
                IncludeLogProbabilities = true,
                User = "LogProbsTester",
                TopLogProbabilityCount = includeTopProbabilities ? 5 : 1
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
