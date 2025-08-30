using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SemanticKernelApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var modelid = "gpt-4o";
            var endpoint = "https://aichatapp-test.openai.azure.com/";
            var apikey = "7VCfS80xweQbGafH4uZoTTs5RdZmmExZOd3eFBVL9tiesEIgxwIIJQQJ99BCACYeBjFXJ3w3AAABACOGm0se";

            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(modelid, endpoint, apikey);

            Kernel kernel = builder.Build();

            //Get reference to the chat completion service
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            var history = new ChatHistory();

            //Define settings for the chat completion
            OpenAIPromptExecutionSettings settings = new()
            {
                ChatSystemPrompt = "You are a helpful assistant that helps people find information. Answer in a friendly manner.",
                MaxTokens = 1500,
                Temperature = 0.9,
                TopP = 0.95,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                StopSequences = new[] { "\n" }
            };

            var reducer = new ChatHistoryTruncationReducer(targetCount: 10);
            //var reducer = new ChatHistorySummarizationReducer(chatCompletionService, 2, 2);

            while (true)
            {
                //Get input from user 
                Console.Write("\n Enter your prompt: ");
                var prompt = Console.ReadLine();

                //Exit if prompt is null or empty
                if (string.IsNullOrEmpty(prompt))
                    break;

                string fullMessage = "";
                //Get token usage information
                OpenAI.Chat.ChatTokenUsage? usage = null;

                //Add user message to chat history  
                history.AddUserMessage(prompt);

                // Get streaming response from chat completion service
                await foreach (StreamingChatMessageContent responseChunk in chatCompletionService.GetStreamingChatMessageContentsAsync(history, settings))
                {
                    //print response to console
                    Console.Write(responseChunk.Content);
                    fullMessage += responseChunk.Content;
                    usage = ((OpenAI.Chat.StreamingChatCompletionUpdate)responseChunk.InnerContent).Usage;
                }

                //add response to history
                history.AddAssistantMessage(fullMessage);

                // Display number of tokens used. Model Specific
                Console.WriteLine($"\n\tInput Tokens: \t{usage?.InputTokenCount}");
                Console.WriteLine($"\tOutput Tokens: \t{usage?.OutputTokenCount}");
                Console.WriteLine($"\tTotal Tokens: \t{usage?.TotalTokenCount}");
                Console.WriteLine($"\t---------------------------------------");
                Console.WriteLine($"\tType another prompt or press enter to exit.");

                //Reduce chat history if it exceeds the target count
                var reduceMessages = await reducer.ReduceAsync(history);
                if (reduceMessages != null)
                    history = new(reduceMessages);
            }
        }
    }
}