namespace ClaimMD.Client.Configuration;

/// <summary>
/// Configuration options for the Claim.MD API client.
/// Bind to appsettings.json under "ClaimMD" or set directly.
/// </summary>
public sealed class ClaimMdOptions
{
    public const string SectionName = "ClaimMD";

    /// <summary>
    /// Your Claim.MD API Account Key.
    /// Located in the portal: Settings → Account Settings → API Key.
    /// Never hardcode this value — use environment variables or secrets management.
    /// </summary>
    public string AccountKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Claim.MD API. Defaults to production.
    /// </summary>
    public string BaseUrl { get; set; } = "https://svc.claim.md";

    /// <summary>
    /// HTTP response format: "application/xml" or "application/json".
    /// Defaults to JSON.
    /// </summary>
    public string AcceptHeader { get; set; } = "application/json";

    /// <summary>
    /// HTTP client timeout in seconds. Defaults to 30.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retries for transient HTTP failures. Defaults to 2.
    /// </summary>
    public int RetryCount { get; set; } = 2;
}
