namespace Shopping.Api.Settings;

public class EmbeddingSettings
{
    public string Provider { get; set; } = "fake"; // fake | ollama | openai
    public OllamaOptions Ollama { get; set; } = new();
    public OpenAiOptions OpenAi { get; set; } = new();

    public class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "nomic-embed-text";
    }

    public class OpenAiOptions
    {
        public string BaseUrl { get; set; } = "https://models.inference.ai.azure.com";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "text-embedding-3-small";
    }
}
