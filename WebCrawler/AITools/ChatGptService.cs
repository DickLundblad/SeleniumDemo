using System.Text;
using Newtonsoft.Json;

public class ChatGPTService
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public ChatGPTService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<string> GetChatResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        const string apiUrl = "https://api.openai.com/v1/chat/completions";

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            max_tokens = 1000
        };

        try
        {
            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(apiUrl, jsonContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Error: {response.StatusCode} - {responseContent}";
            }

            var responseObject = JsonConvert.DeserializeObject<ChatResponse>(responseContent);
            return responseObject?.Choices[0].Message.Content.Trim() ?? "No response";
        }
        catch (Exception ex)
        {
            return $"Exception: {ex.Message}";
        }
    }

    // Response classes
    private class ChatResponse
    {
        public Choice[] Choices { get; set; }
    }

    private class Choice
    {
        public Message Message { get; set; }
    }

    private class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}


