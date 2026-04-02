using Microsoft.AspNetCore.Mvc;
using Shopping.Api.Services.Implementations;
using System.Text.Json;

namespace Shopping.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
public class RecommendationsController(RecommendationService recommendationService) : ControllerBase
{
    [HttpGet("stream")]
    public async Task StreamAsync([FromQuery] string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        await foreach (var (eventType, data) in recommendationService.StreamAsync(userId, ct))
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await Response.WriteAsync($"event: {eventType}\ndata: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }

    [HttpGet("stream/search")]
    public async Task StreamByPromptAsync([FromQuery] string prompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        await foreach (var (eventType, data) in recommendationService.StreamByPromptAsync(prompt, ct))
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await Response.WriteAsync($"event: {eventType}\ndata: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
