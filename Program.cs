using Microsoft.EntityFrameworkCore;
using Shopping.Api.Data;
using Shopping.Api.Repositories;
using Shopping.Api.Scripts;
using Shopping.Api.Services.Abstractions;
using Shopping.Api.Services.Implementations;
using Shopping.Api.Settings;

var builder = WebApplication.CreateBuilder(args);

// ── Settings ──────────────────────────────────────────────────────────
var llmSettings = builder.Configuration.GetSection("LlmSettings").Get<LlmSettings>()
                  ?? new LlmSettings();
var embeddingSettings = builder.Configuration.GetSection("EmbeddingSettings").Get<EmbeddingSettings>()
                        ?? new EmbeddingSettings();

builder.Services.AddSingleton(llmSettings);
builder.Services.AddSingleton(embeddingSettings);

// ── Database ──────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.UseVector()));

// ── CORS ──────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        var origins = builder.Configuration
                             .GetSection("Cors:AllowedOrigins")
                             .Get<string[]>() ?? [];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── LLM & Embedding (Strategy Pattern) ───────────────────────────────
builder.Services.AddHttpClient();

builder.Services.AddScoped<ILlmService>(sp =>
    llmSettings.Provider.ToLower() switch
    {
        "ollama" => new OllamaLlmService(sp.GetRequiredService<IHttpClientFactory>().CreateClient(), llmSettings),
        _ => new FakeLlmService()
    });

builder.Services.AddScoped<IEmbeddingService>(sp =>
    embeddingSettings.Provider.ToLower() switch
    {
        "ollama" => new OllamaEmbeddingService(sp.GetRequiredService<IHttpClientFactory>().CreateClient(), embeddingSettings),
        _ => new FakeEmbeddingService()
    });

// ── Repositories & Services ───────────────────────────────────────────
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<RecommendationService>();

// ── API ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("FrontendPolicy");
app.MapControllers();

// ── CLI Commands ──────────────────────────────────────────────────────
// Usage: dotnet run -- seed | embed
if (args.Contains("seed"))
{
    await SeedData.RunAsync(app.Services);
    return;
}

if (args.Contains("embed"))
{
    await GenerateEmbeddings.RunAsync(app.Services);
    return;
}

app.Run();

