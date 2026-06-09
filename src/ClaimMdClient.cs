using ClaimMD.Client.Configuration;
using ClaimMD.Client.Models;
using ClaimMD.Client.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ClaimMD.Client;

/// <summary>
/// A single entry-point client for the Claim.MD API.
/// Use this when you don't have a DI container, or as a convenience wrapper.
///
/// For DI-based applications, prefer registering individual services via
/// <c>services.AddClaimMd(...)</c> and injecting <see cref="IEligibilityService"/>,
/// <see cref="IClaimService"/>, <see cref="IEraService"/>, and <see cref="IPayerService"/>.
/// </summary>
public sealed class ClaimMdClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    /// <summary>Real-time eligibility checks (JSON and X12 270/271).</summary>
    public IEligibilityService Eligibility { get; }

    /// <summary>Claim file upload and status polling.</summary>
    public IClaimService Claims { get; }

    /// <summary>Electronic Remittance Advice retrieval.</summary>
    public IEraService Era { get; }

    /// <summary>Payer list and search.</summary>
    public IPayerService Payers { get; }

    /// <summary>
    /// Creates a new <see cref="ClaimMdClient"/> with an account key.
    /// </summary>
    /// <param name="accountKey">Your Claim.MD API account key.</param>
    /// <param name="loggerFactory">
    /// Optional logger factory. Pass null to suppress all logging.
    /// </param>
    public ClaimMdClient(string accountKey, ILoggerFactory? loggerFactory = null)
        : this(new ClaimMdOptions { AccountKey = accountKey }, loggerFactory) { }

    /// <summary>
    /// Creates a new <see cref="ClaimMdClient"/> with full options.
    /// </summary>
    public ClaimMdClient(ClaimMdOptions options, ILoggerFactory? loggerFactory = null)
    {
        if (string.IsNullOrWhiteSpace(options.AccountKey))
            throw new ArgumentException("AccountKey must be provided.", nameof(options));

        loggerFactory ??= NullLoggerFactory.Instance;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
            Timeout     = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };

        var optionsWrapper = new OptionsWrapper<ClaimMdOptions>(options);

        Eligibility = new EligibilityService(_httpClient, optionsWrapper,
            loggerFactory.CreateLogger<EligibilityService>());

        Claims = new ClaimService(_httpClient, optionsWrapper,
            loggerFactory.CreateLogger<ClaimService>());

        Era = new EraService(_httpClient, optionsWrapper,
            loggerFactory.CreateLogger<EraService>());

        Payers = new PayerService(_httpClient, optionsWrapper,
            loggerFactory.CreateLogger<PayerService>());
    }

    /// <summary>
    /// Convenience: check eligibility in one call.
    /// </summary>
    public Task<EligibilityResponse> CheckEligibilityAsync(
        EligibilityRequest request,
        CancellationToken cancellationToken = default)
        => Eligibility.CheckEligibilityAsync(request, cancellationToken);

    /// <summary>
    /// Convenience: submit a claim file in one call.
    /// </summary>
    public Task<ClaimUploadResponse> SubmitClaimAsync(
        byte[] fileBytes,
        string filename,
        CancellationToken cancellationToken = default)
        => Claims.SubmitClaimFileAsync(fileBytes, filename, cancellationToken);

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient.Dispose();
        _disposed = true;
    }
}
