using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
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

    public Task<string> AnalyzeShowdownSetAsync(string showdownSet, string context)
    {
        return AnalyzeShowdownSetAsync(showdownSet, context, CancellationToken.None);
    }

    public async Task<string> AnalyzeShowdownSetAsync(string showdownSet, string context, CancellationToken cancellationToken)
    {
        try
        {
            var systemPrompt = @"You are an expert Pokémon team builder and competitive player with deep knowledge of Pokémon legality rules, breeding mechanics, and the AutoLegalityMod plugin for PKHeX. 

                                Your task is to analyze Showdown sets and help users understand if their Pokémon is legal or not.

                                CRITICAL RULES:
                                1. ALWAYS check the 'Legalization Status' and 'Is Legal' fields FIRST
                                2. If Status is 'Regenerated' and Is Legal is 'True', the Pokémon is ALREADY LEGAL - don't invent issues!
                                3. Only suggest fixes if there are actual legality problems
                                4. Use the provided valid data (abilities, moves, balls) from the context - these are ACCURATE
                                5. The 'VALID ABILITIES' section shows ALL abilities the Pokémon can have - trust this list completely
                                6. Never claim an ability/move is invalid if it's listed in the VALID sections
                                7. Keep explanations brief and accurate
                                8. Format your response clearly with proper spacing and line breaks

                                IMPORTANT: Trust the PKHeX legality check - if it says the Pokémon is legal, then it IS legal!";

                                            var userPrompt = $@"Analyze this Showdown set:

                                Showdown Set:
                                {showdownSet}

                                Legality Check Results:
                                {context}

                                IMPORTANT: Check the legalization status first!
                                - If 'Legalization Status: Regenerated' AND 'Is Legal: True', then the set is ALREADY LEGAL
                                - Only suggest fixes if the status is NOT Regenerated or Is Legal is False

                                IF THE SET IS LEGAL (Status: Regenerated, Is Legal: True):
                                Just respond with:
                                == STATUS ==
                                ✓ This Showdown set is legal and ready to use!

                                The Pokémon was successfully generated and passes all legality checks.

                                IF THE SET HAS ISSUES:
                                == ISSUES FOUND ==

                                • Issue 1: [Brief description]
                                • Issue 2: [Brief description]

                                == QUICK FIXES ==

                                • For Issue 1: [Specific fix using valid options from above]
                                • For Issue 2: [Specific fix using valid options from above]

                                == CORRECTED SHOWDOWN SET ==

                                [Pokémon Name] @ [Item]
                                Level: [X]
                                Ability: [Valid Ability]
                                EVs: [Valid spread]
                                [Nature] Nature
                                - [Move 1]
                                - [Move 2]
                                - [Move 3]
                                - [Move 4]

                                CRITICAL: Always check the 'VALID ABILITIES' section in the context before claiming an ability is invalid!";

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

            var response = await _httpClient.PostAsync(OpenAIEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"OpenAI API error: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseJson = JsonDocument.Parse(responseContent);

            var aiResponse = responseJson.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return aiResponse ?? "No response from AI.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return $"Error communicating with AI service: {ex.Message}";
        }
    }
}