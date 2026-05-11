namespace ComplianceHub.Application.Features.RegulationDetails;

public static class AiProviderMetadata
{
    public static string NormalizeProvider(string provider) =>
        provider.Trim().ToLowerInvariant() switch
        {
            "azure-openai" => "azure-openai",
            "gemini" => "gemini",
            "claude" => "claude",
            "openai" => "openai",
            "openrouter" => "openrouter",
            _ => throw new ArgumentException("Unsupported AI provider.")
        };

    public static string GetProviderDisplayName(string provider) =>
        provider switch
        {
            "azure-openai" => "Azure OpenAI Service",
            "gemini" => "Google Gemini API",
            "claude" => "Anthropic Claude API",
            "openai" => "OpenAI",
            "openrouter" => "OpenRouter",
            _ => provider
        };
}
