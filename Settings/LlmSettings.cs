namespace Shopping.Api.Settings;

public class LlmSettings
{
    public string Provider { get; set; } = "fake"; // fake | ollama | openai
    public OllamaOptions Ollama { get; set; } = new();
    public OpenAiOptions OpenAi { get; set; } = new();

    public class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "qwen2.5:7b";
    }

    public class OpenAiOptions
    {
        public string BaseUrl { get; set; } = "https://models.inference.ai.azure.com";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o-mini";
    }
}
