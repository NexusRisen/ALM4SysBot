using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AutoModPlugins.GUI;

public class AIService(string apiKey, string model, int maxTokens, double temperature)
{
    private readonly string _apiKey = apiKey;
    private readonly string _model = model;
    private readonly int _maxTokens = maxTokens;
    private readonly double _temperature = temperature;
    private static readonly HttpClient _sharedHttpClient = new();

    private const string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";

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

            using var request = new HttpRequestMessage(HttpMethod.Post, OpenAIEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = content;

            using var response = await _sharedHttpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                var statusCode = (int)response.StatusCode;

                if (statusCode == 401)
                    throw new Exception("Invalid API key. Please check your OpenAI API key in settings.");
                else if (statusCode == 429)
                    throw new Exception("Rate limit exceeded. Please try again later.");
                else if (statusCode >= 500)
                    throw new Exception("OpenAI service is temporarily unavailable. Please try again later.");

                throw new Exception($"OpenAI API error: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            using var responseJson = JsonDocument.Parse(responseContent);

            if (!responseJson.RootElement.TryGetProperty("choices", out var choices) ||
                choices.GetArrayLength() == 0 ||
                !choices[0].TryGetProperty("message", out var message) ||
                !message.TryGetProperty("content", out var contentElement))
            {
                throw new Exception("Unexpected response format from OpenAI API.");
            }

            var aiResponse = contentElement.GetString();
            return aiResponse ?? "No response from AI.";
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            throw new OperationCanceledException("Analysis was cancelled.", ex, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return "Request timed out. Please try again.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            return $"Network error: {ex.Message}. Please check your internet connection.";
        }
        catch (JsonException)
        {
            return "Failed to parse API response. The service may be experiencing issues.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}