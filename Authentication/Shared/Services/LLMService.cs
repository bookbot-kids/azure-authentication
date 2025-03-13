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

        public async Task<string> GetNameFromEmail(string email)
        {
            var messages = new List<ChatMessage>
            {
                new UserChatMessage($@"
I'd like you to extract the name from this email address email in a json response. Here is an example:
Input: kellydhart@yahoo.com
Output:
{{
first_name: ""Kelly"",
last_name: ""Hart""
}}
What is the output of this email address: {email}?
It must be a real name, otherwise keep it empty. No explanation. Only show a json response.
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
                        string lastName = root.GetProperty("last_name").GetString();
                        return StringHelper.CombineName(firstName, lastName);
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

