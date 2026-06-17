using System.Net.Http.Headers;
using System.Text;
using McpServerDotnet.Servers.AzureDevOps.Options;
using Microsoft.Extensions.Options;

namespace McpServerDotnet.Servers.AzureDevOps.Services;

/// <summary>
/// Thin wrapper around <see cref="HttpClient"/> that pre-configures base URL,
/// Authorization header, and API-version query parameters for the Azure DevOps
/// REST API.
/// </summary>
public sealed class AzureDevOpsHttpClient
{
    private readonly HttpClient _http;
    private readonly AzureDevOpsOptions _options;

    /// <summary>Initializes a new <see cref="AzureDevOpsHttpClient"/>.</summary>
    public AzureDevOpsHttpClient(HttpClient http, IOptions<AzureDevOpsOptions> options)
    {
        _options = options.Value;
        _http = http;
        _http.BaseAddress = new Uri($"https://dev.azure.com/{_options.Organization}/");
        _http.Timeout = _options.RequestTimeout;

        if (!string.IsNullOrWhiteSpace(_options.PersonalAccessToken))
        {
            var token = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($":{_options.PersonalAccessToken}"));
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", token);
        }

        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>Builds a URL relative to the configured organization.</summary>
    public string BuildUrl(string path, string? extraQuery = null)
    {
        var versionParam = $"api-version={_options.ApiVersion}";
        var query = extraQuery is null ? versionParam : $"{extraQuery}&{versionParam}";
        return $"{path}?{query}";
    }

    /// <summary>Performs a GET request and returns the raw JSON string.</summary>
    public async Task<string> GetStringAsync(string relativeUrl, CancellationToken ct = default)
        => await _http.GetStringAsync(relativeUrl, ct);

    /// <summary>Performs a PATCH request with a JSON body.</summary>
    public async Task<HttpResponseMessage> PatchJsonAsync(
        string relativeUrl, string jsonBody, CancellationToken ct = default)
    {
        using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json-patch+json");
        return await _http.PatchAsync(relativeUrl, content, ct);
    }

    /// <summary>Performs a POST request with a JSON body.</summary>
    public async Task<HttpResponseMessage> PostJsonAsync(
        string relativeUrl, string jsonBody, CancellationToken ct = default)
    {
        using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        return await _http.PostAsync(relativeUrl, content, ct);
    }
}
