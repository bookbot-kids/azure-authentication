using System;
using OpenAI.Chat;
using System.Text.Json;
using System.Threading.Tasks;
using Authentication.Shared.Library;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Extensions;

namespace Authentication.Shared.Services
{
	public class LLMService
	{
        public static LLMService Instance { get; } = new LLMService();
        private readonly ChatClient client;

        private LLMService()
        {
            var apiKey = Configurations.OpenAI.Key;
            var model = Configurations.OpenAI.Model;
            if(!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(model))
            {
                client = new ChatClient(model, apiKey);
            }            
        }

        /// <summary>
        /// Sends a prompt to OpenAI and returns the raw text
        /// </summary>
        public async Task<string> GetContentAsync(List<ChatMessage> messages, ChatCompletionOptions options)
        {
            if(client == null)
            {
                return "";
            }

            try
            {
                var completion = await client.CompleteChatAsync(messages, options);
                var response = completion.Value.Content[0].Text;          
                return response;
            }
            catch (Exception ex)
            {
                Logger.Log?.LogError($"Error communicating with OpenAI. ${ex.Message}");
                return "";
            }
        }

        public async Task<string> GetNameFromEmail(string email, string country = "")
        {
            var messages = new List<ChatMessage>
            {
                new UserChatMessage($@"
    For the following email entry, provide the likely first name (only the first name) by inferring from the part before the '@' in the email address. Use the provided Country as context. Answer with ONLY the first name without numbers or prefixes.
    Email: {email} Country: {country}
    Answers (first names only) in json format:
    {{
      ""first_name"": """"
    }}
"
)
            };

            var response = await GetContentAsync(messages, new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            });

            if (!string.IsNullOrWhiteSpace(response))
            {
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        // Get the root element
                        JsonElement root = doc.RootElement;

                        // Extract the first name and last name
                        string firstName = root.GetProperty("first_name").GetString();
                        return firstName ?? "";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log?.LogError($"Error parsing json ${ex.Message}");
                }
            }

            return "";
        }
    }
}

