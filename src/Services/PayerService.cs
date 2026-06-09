using ClaimMD.Client.Configuration;
using ClaimMD.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClaimMD.Client.Services;

/// <inheritdoc cref="IPayerService"/>
internal sealed class PayerService : ClaimMdServiceBase, IPayerService
{
    public PayerService(
        HttpClient httpClient,
        IOptions<ClaimMdOptions> options,
        ILogger<PayerService> logger)
        : base(httpClient, options, logger) { }

    /// <inheritdoc/>
    public async Task<PayerListResponse> GetPayersAsync(
        string? payerId = null,
        CancellationToken cancellationToken = default)
    {
        var fields = new Dictionary<string, string>();

        if (payerId is not null)
            fields["PayerID"] = payerId;

        Logger.LogDebug("Fetching payer list. Filter={PayerId}", payerId ?? "all");

        return await PostFormAsync<PayerListResponse>(
            "services/payerlist/", fields, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<PayerInfo>> SearchPayersByNameAsync(
        string nameContains,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nameContains))
            throw new ArgumentException("Search term cannot be empty.", nameof(nameContains));

        var allPayers = await GetPayersAsync(null, cancellationToken);

        return (allPayers.Payers ?? [])
            .Where(p => p.PayerName?.Contains(nameContains, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();
    }
}
