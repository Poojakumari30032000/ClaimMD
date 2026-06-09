using ClaimMD.Client.Configuration;
using ClaimMD.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClaimMD.Client.Services;

/// <inheritdoc cref="IClaimService"/>
internal sealed class ClaimService : ClaimMdServiceBase, IClaimService
{
    public ClaimService(
        HttpClient httpClient,
        IOptions<ClaimMdOptions> options,
        ILogger<ClaimService> logger)
        : base(httpClient, options, logger) { }

    /// <inheritdoc/>
    public async Task<ClaimUploadResponse> SubmitClaimFileAsync(
        byte[] fileContent,
        string filename,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileContent);
        if (fileContent.Length == 0) throw new ArgumentException("File content cannot be empty.", nameof(fileContent));
        ValidateFilename(filename);

        using var stream = new MemoryStream(fileContent);
        return await SubmitClaimFileAsync(stream, filename, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ClaimUploadResponse> SubmitClaimFileAsync(
        Stream fileStream,
        string filename,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        ValidateFilename(filename);

        Logger.LogInformation("Uploading claim file: {Filename}", filename);

        return await PostMultipartAsync<ClaimUploadResponse>(
            "services/upload/", fileStream, filename, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ClaimStatusResponse> GetClaimStatusUpdatesAsync(
        string responseId,
        string? claimMdId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(responseId))
            throw new ArgumentException("ResponseId cannot be empty. Use '0' for the first call.", nameof(responseId));

        var fields = new Dictionary<string, string>
        {
            ["ResponseID"] = responseId
        };

        if (claimMdId is not null)
            fields["ClaimID"] = claimMdId;

        Logger.LogInformation("Polling claim status updates since ResponseID={ResponseId}", responseId);

        return await PostFormAsync<ClaimStatusResponse>(
            "services/response/", fields, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UploadedFilesResponse> GetUploadedFilesAsync(
        string? uploadDate = null,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var fields = new Dictionary<string, string>
        {
            ["Page"] = page.ToString()
        };

        if (uploadDate is not null)
            fields["UploadDate"] = uploadDate;

        Logger.LogDebug("Fetching uploaded file list: page={Page} date={Date}", page, uploadDate ?? "any");

        return await PostFormAsync<UploadedFilesResponse>(
            "services/uploadlist/", fields, cancellationToken);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".837", ".txt", ".xml", ".json", ".csv", ".xls", ".xlsx", ".pdf", ".x12"
        };

    private static void ValidateFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            throw new ArgumentException("Filename cannot be empty.", nameof(filename));

        var ext = Path.GetExtension(filename);
        if (!SupportedExtensions.Contains(ext))
            throw new ArgumentException(
                $"Unsupported file extension '{ext}'. Supported: {string.Join(", ", SupportedExtensions)}",
                nameof(filename));
    }
}
