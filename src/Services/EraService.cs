using ClaimMD.Client.Configuration;
using ClaimMD.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClaimMD.Client.Services;

/// <inheritdoc cref="IEraService"/>
internal sealed class EraService : ClaimMdServiceBase, IEraService
{
    public EraService(
        HttpClient httpClient,
        IOptions<ClaimMdOptions> options,
        ILogger<EraService> logger)
        : base(httpClient, options, logger) { }

    /// <inheritdoc/>
    public async Task<EraListResponse> GetEraListAsync(
        EraListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fields = new Dictionary<string, string>
        {
            ["ERAID"] = request.EraId
        };

        if (request.PayerId is not null)
            fields["PayerID"] = request.PayerId;

        Logger.LogInformation("Fetching ERA list since EraID={EraId}", request.EraId);

        return await PostFormAsync<EraListResponse>(
            "services/eralist/", fields, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<EraDetailResponse> GetEraDetailAsync(
        string eraId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eraId))
            throw new ArgumentException("EraId cannot be empty.", nameof(eraId));

        Logger.LogInformation("Fetching ERA detail for EraID={EraId}", eraId);

        var fields = new Dictionary<string, string> { ["ERAID"] = eraId };
        return await PostFormAsync<EraDetailResponse>(
            "services/eradata/", fields, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string> GetEra835Async(
        string eraId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eraId))
            throw new ArgumentException("EraId cannot be empty.", nameof(eraId));

        Logger.LogInformation("Downloading 835 for EraID={EraId}", eraId);

        var fields = new Dictionary<string, string> { ["ERAID"] = eraId };
        return await PostFormRawStringAsync("services/era835/", fields, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<byte[]> GetEraPdfAsync(
        string eraId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eraId))
            throw new ArgumentException("EraId cannot be empty.", nameof(eraId));

        Logger.LogInformation("Downloading ERA PDF for EraID={EraId}", eraId);

        var fields = new Dictionary<string, string> { ["ERAID"] = eraId };
        return await PostFormRawBytesAsync("services/erapdf/", fields, cancellationToken);
    }
}
