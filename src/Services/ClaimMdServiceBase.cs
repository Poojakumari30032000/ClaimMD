using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClaimMD.Client.Configuration;
using ClaimMD.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Xml.Linq;

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
        // Request XML — this is what Claim.MD actually returns
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        request.Content = new FormUrlEncodedContent(fields);

        Logger.LogDebug("POST {Endpoint}", endpoint);

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

        if (!response.IsSuccessStatusCode)
            throw new ClaimMdApiException($"HTTP {(int)response.StatusCode}: {raw}");

        // Claim.MD returns XML regardless of Accept header in most endpoints
        var cleaned = new string(raw.Where(c => c >= 0x20 || c == '\t' || c == '\n' || c == '\r').ToArray());
        // Prefix attribute names that start with a digit (e.g. 1500_claims → _1500_claims)
        var sanitized = System.Text.RegularExpressions.Regex.Replace(cleaned, @"(\s)(\d)", "$1_$2");
        var doc = System.Xml.Linq.XDocument.Parse(sanitized);
        var root = doc.Root!;

        // Check for errors first
        var errorEls = root.Elements("error").ToList();
        if (errorEls.Any())
        {
            var errors = errorEls.Select(e => new ApiError
            {
                Code = e.Attribute("error_code")?.Value,
                Message = e.Attribute("error_mesg")?.Value ?? e.Value
            }).ToList();
            return (T)CreateErrorResult<T>(errors);
        }

        // Convert XML to JSON then deserialize — handles all response types uniformly
        var json = System.Text.Json.JsonSerializer.Serialize(XmlToDict(root));
        Logger.LogTrace("Converted JSON: {Json}", json);

        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new ClaimMdApiException("Empty response.");
    }

    private static Dictionary<string, object?> XmlToDict(System.Xml.Linq.XElement el)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var attr in el.Attributes())
            dict[attr.Name.LocalName] = (object?)attr.Value;

        var groups = el.Elements().GroupBy(e => e.Name.LocalName);
        foreach (var group in groups)
        {
            var items = group.Select(e => (object?)XmlToDict(e)).ToList();
            dict[group.Key] = (object?)items;
        }

        return dict;
    }

    private static object CreateErrorResult<T>(List<ApiError> errors) where T : class
    {
        // All response types have an Errors property — set it via reflection
        var instance = Activator.CreateInstance<T>();
        var prop = typeof(T).GetProperty("Errors");
        prop?.SetValue(instance, errors);
        return instance;
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
