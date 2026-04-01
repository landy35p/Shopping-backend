namespace Shopping.Api.Settings;

public class EmbeddingSettings
{
    public string Provider { get; set; } = "fake"; // fake | ollama
    public OllamaOptions Ollama { get; set; } = new();

    public class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "nomic-embed-text";
    }
}
