using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoModPlugins.GUI;

public class AIService
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxTokens;
    private readonly double _temperature;
    private readonly HttpClient _httpClient;

    private const string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";

    public AIService(string apiKey, string model, int maxTokens, double temperature)
    {
        _apiKey = apiKey;
        _model = model;
        _maxTokens = maxTokens;
        _temperature = temperature;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<string> AnalyzeShowdownSetAsync(string showdownSet, string context)
    {
        try
        {
            var systemPrompt = @"You are an expert Pokémon team builder and competitive player with deep knowledge of Pokémon legality rules, breeding mechanics, and the AutoLegalityMod plugin for PKHeX. 

                                Your task is to analyze Showdown sets and provide helpful, friendly advice on how to fix any legality issues. Focus on:
                                1. Explaining what's wrong in simple terms
                                2. Providing specific steps to fix the issue
                                3. Suggesting alternatives if something is impossible
                                4. Being encouraging and helpful

                                IMPORTANT: Format your response for display in a plain text box. Do NOT use markdown formatting like ###, **, `, or ```. Instead:
                                - Use clear section headers with text like 'SECTION NAME:' or '== SECTION NAME =='
                                - Use bullet points with • or - 
                                - Use numbered lists like 1. 2. 3.
                                - Separate sections with blank lines
                                - Keep formatting simple and readable";

                                            var userPrompt = $@"Please analyze this Showdown set and help me understand any issues and how to fix them:

                                ```
                                {showdownSet}
                                ```

                                Context from legality check:
                                {context}

                                Please provide:
                                1. A summary of any issues found
                                2. Step-by-step instructions to fix each issue
                                3. Alternative suggestions if something cannot be fixed
                                4. Any tips for avoiding similar issues in the future";

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = _maxTokens,
                temperature = _temperature
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(OpenAIEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI API error: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseJson = JsonDocument.Parse(responseContent);

            var aiResponse = responseJson.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return aiResponse ?? "No response from AI.";
        }
        catch (Exception ex)
        {
            return $"Error communicating with AI service: {ex.Message}";
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}