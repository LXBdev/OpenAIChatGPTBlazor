using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAIChatGPTBlazor.Services.Models;

namespace OpenAIChatGPTBlazor.Services;

public interface IVideoGenerationService
{
    public Task<VideoJobResponse> CreateVideoJobAsync(
        VideoGenerationRequest request,
        CancellationToken cancellationToken = default
    );
    public Task<VideoJobStatusResponse> GetJobStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default
    );
    public Task<byte[]> GetVideoContentAsync(
        string generationId,
        CancellationToken cancellationToken = default
    );
}

public class VideoGenerationService : IVideoGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIClient _openAIClient;
    private readonly ILogger<VideoGenerationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _baseUrl;
    private readonly string? _apiKey;
    private readonly bool _useApiKey;

    public VideoGenerationService(
        HttpClient httpClient,
        [FromKeyedServices("OpenAi_Video")] OpenAIClient openAIClient,
        ILogger<VideoGenerationService> logger,
        IConfiguration configuration
    )
    {
        _httpClient = httpClient;
        _openAIClient = openAIClient;
        _logger = logger;
        _configuration = configuration;

        // Parse connection string to extract endpoint and API key
        var connectionString = configuration.GetConnectionString("OpenAi_Video");
        (_baseUrl, _apiKey, _useApiKey) = ParseConnectionString(connectionString);
    }

    private (string baseUrl, string? apiKey, bool useApiKey) ParseConnectionString(
        string? connectionString
    )
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            // Fallback to extracting from OpenAI client
            return (ExtractBaseUrlFromClient(), null, false);
        }

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        string? endpoint = null;
        string? key = null;

        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2)
            {
                var paramName = keyValue[0].Trim();
                var paramValue = keyValue[1].Trim();

                if (paramName.Equals("Endpoint", StringComparison.OrdinalIgnoreCase))
                {
                    endpoint = paramValue.TrimEnd('/');
                }
                else if (paramName.Equals("Key", StringComparison.OrdinalIgnoreCase))
                {
                    key = paramValue;
                }
            }
        }

        var baseUrl = endpoint ?? ExtractBaseUrlFromClient();
        var useApiKey = !string.IsNullOrEmpty(key);

        _logger.LogInformation(
            "Video service configured with {AuthMethod} authentication",
            useApiKey ? "API Key" : "Managed Identity"
        );

        return (baseUrl, key, useApiKey);
    }

    private string ExtractBaseUrlFromClient()
    {
        // Extract the endpoint from the OpenAI client
        // This assumes the client is configured with Azure OpenAI endpoint
        try
        {
            var endpoint = _openAIClient
                .GetType()
                .GetProperty("Endpoint")
                ?.GetValue(_openAIClient)
                ?.ToString();

            if (!string.IsNullOrEmpty(endpoint))
            {
                return endpoint.TrimEnd('/');
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract endpoint from OpenAI client");
        }

        // Fallback to configuration or default
        return "https://oai-alex-sdc.openai.azure.com";
    }

    private async Task SetAuthenticationHeaderAsync(HttpRequestMessage httpRequest)
    {
        if (_useApiKey && !string.IsNullOrEmpty(_apiKey))
        {
            httpRequest.Headers.Add("Api-key", _apiKey);
        }
        else
        {
            try
            {
                // Use DefaultAzureCredential for managed identity authentication
                var credential = new Azure.Identity.DefaultAzureCredential();
                var tokenRequestContext = new TokenRequestContext(
                    new[] { "https://cognitiveservices.azure.com/.default" }
                );
                var accessToken = await credential.GetTokenAsync(
                    tokenRequestContext,
                    CancellationToken.None
                );
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken.Token
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get bearer token using DefaultAzureCredential");
                throw new InvalidOperationException(
                    "Could not obtain bearer token for video generation",
                    ex
                );
            }
        }
    }

    public async Task<VideoJobResponse> CreateVideoJobAsync(
        VideoGenerationRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var url = $"{_baseUrl}/openai/v1/video/generations/jobs?api-version=preview";

            var jsonContent = JsonSerializer.Serialize(
                request,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }
            );

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json"),
            };

            // Set authentication header based on configuration
            await SetAuthenticationHeaderAsync(httpRequest);

            _logger.LogInformation(
                "Creating video generation job for prompt: {Prompt}",
                request.Prompt
            );

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var jobResponse = JsonSerializer.Deserialize<VideoJobResponse>(
                    responseContent,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    }
                );

                _logger.LogInformation(
                    "Video generation job created successfully. Job ID: {JobId}",
                    jobResponse?.JobId
                );
                return jobResponse
                    ?? throw new InvalidOperationException("Failed to deserialize job response");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to create video generation job. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode,
                    errorContent
                );
                throw new HttpRequestException(
                    $"Video generation job creation failed: {response.StatusCode} - {errorContent}"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating video generation job");
            throw;
        }
    }

    public async Task<VideoJobStatusResponse> GetJobStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var url = $"{_baseUrl}/openai/v1/video/generations/jobs/{jobId}?api-version=preview";

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            await SetAuthenticationHeaderAsync(httpRequest);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var statusResponse = JsonSerializer.Deserialize<VideoJobStatusResponse>(
                    responseContent,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    }
                );

                return statusResponse
                    ?? throw new InvalidOperationException("Failed to deserialize status response");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to get job status for {JobId}. Status: {StatusCode}, Content: {Content}",
                    jobId,
                    response.StatusCode,
                    errorContent
                );
                throw new HttpRequestException(
                    $"Failed to get job status: {response.StatusCode} - {errorContent}"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job status for {JobId}", jobId);
            throw;
        }
    }

    public async Task<byte[]> GetVideoContentAsync(
        string generationId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var url =
                $"{_baseUrl}/openai/v1/video/generations/{generationId}/content/video?api-version=preview";

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            await SetAuthenticationHeaderAsync(httpRequest);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var videoContent = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                _logger.LogInformation(
                    "Retrieved video content for generation {GenerationId}. Size: {Size} bytes",
                    generationId,
                    videoContent.Length
                );
                return videoContent;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to get video content for {GenerationId}. Status: {StatusCode}, Content: {Content}",
                    generationId,
                    response.StatusCode,
                    errorContent
                );
                throw new HttpRequestException(
                    $"Failed to get video content: {response.StatusCode} - {errorContent}"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video content for {GenerationId}", generationId);
            throw;
        }
    }
}
