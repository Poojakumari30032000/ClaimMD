using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClaimMD.Client.Configuration;
using ClaimMD.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClaimMD.Client.Services;

/// <summary>
/// Base HTTP wrapper for the Claim.MD REST API.
/// Handles authentication, content negotiation, and error parsing.
/// </summary>
internal abstract class ClaimMdServiceBase
{
    protected readonly HttpClient HttpClient;
    protected readonly ClaimMdOptions Options;
    protected readonly ILogger Logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    protected ClaimMdServiceBase(
        HttpClient httpClient,
        IOptions<ClaimMdOptions> options,
        ILogger logger)
    {
        HttpClient = httpClient;
        Options = options.Value;
        Logger = logger;

        HttpClient.BaseAddress = new Uri(Options.BaseUrl.TrimEnd('/') + "/");
        HttpClient.Timeout = TimeSpan.FromSeconds(Options.TimeoutSeconds);
    }

    // ── Form-encoded POST (most Claim.MD endpoints) ───────────────────

    /// <summary>
    /// POST application/x-www-form-urlencoded and deserialize the JSON response.
    /// AccountKey is automatically injected.
    /// </summary>
    protected async Task<T> PostFormAsync<T>(
        string endpoint,
        Dictionary<string, string> fields,
        CancellationToken ct) where T : class
    {
        fields["AccountKey"] = Options.AccountKey;

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new FormUrlEncodedContent(fields);

        Logger.LogDebug("POST {Endpoint} fields={Fields}", endpoint, string.Join(", ", fields.Keys.Where(k => k != "AccountKey")));

        using var response = await HttpClient.SendAsync(request, ct);
        return await ParseResponseAsync<T>(response, ct);
    }

    // ── Multipart POST (file upload) ──────────────────────────────────

    /// <summary>
    /// POST multipart/form-data for file upload and deserialize the JSON response.
    /// AccountKey is automatically injected.
    /// </summary>
    protected async Task<T> PostMultipartAsync<T>(
        string endpoint,
        Stream fileStream,
        string filename,
        CancellationToken ct) where T : class
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(Options.AccountKey), "AccountKey");

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(filename));
        content.Add(fileContent, "File", filename);
        content.Add(new StringContent(filename), "Filename");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = content;

        Logger.LogDebug("POST {Endpoint} file={Filename}", endpoint, filename);

        using var response = await HttpClient.SendAsync(request, ct);
        return await ParseResponseAsync<T>(response, ct);
    }

    // ── Raw response (835, 271, PDF) ──────────────────────────────────

    /// <summary>
    /// POST and return the raw response as a string (for X12 835 / 271).
    /// </summary>
    protected async Task<string> PostFormRawStringAsync(
        string endpoint,
        Dictionary<string, string> fields,
        CancellationToken ct)
    {
        fields["AccountKey"] = Options.AccountKey;

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new FormUrlEncodedContent(fields);

        using var response = await HttpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    /// <summary>
    /// POST and return the raw response as bytes (for PDF).
    /// </summary>
    protected async Task<byte[]> PostFormRawBytesAsync(
        string endpoint,
        Dictionary<string, string> fields,
        CancellationToken ct)
    {
        fields["AccountKey"] = Options.AccountKey;

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new FormUrlEncodedContent(fields);

        using var response = await HttpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    // ── Response parsing ──────────────────────────────────────────────

    private async Task<T> ParseResponseAsync<T>(HttpResponseMessage response, CancellationToken ct) where T : class
    {
        var raw = await response.Content.ReadAsStringAsync(ct);
        Logger.LogTrace("Response [{StatusCode}]: {Body}", (int)response.StatusCode, raw);

        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("HTTP {StatusCode} from Claim.MD: {Body}", (int)response.StatusCode, raw);
            throw new ClaimMdApiException(
                $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}. Body: {raw}");
        }

        T? result;
        try
        {
            result = JsonSerializer.Deserialize<T>(raw, JsonOptions);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to parse Claim.MD response: {Body}", raw);
            throw new ClaimMdApiException($"Failed to deserialize response: {ex.Message}", ex);
        }

        if (result is null)
            throw new ClaimMdApiException("Claim.MD returned an empty response.");

        return result;
    }

    private static string GetContentType(string filename) =>
        Path.GetExtension(filename).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".xml" => "application/xml",
            ".json" => "application/json",
            ".csv" => "text/csv",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
}
