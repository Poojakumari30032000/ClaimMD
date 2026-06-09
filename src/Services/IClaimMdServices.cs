using ClaimMD.Client.Models;

namespace ClaimMD.Client.Services;

// ─────────────────────────────────────────────
//  ELIGIBILITY SERVICE
// ─────────────────────────────────────────────

/// <summary>
/// Performs real-time eligibility checks against the Claim.MD API.
/// </summary>
public interface IEligibilityService
{
    /// <summary>
    /// Check eligibility using structured JSON parameters.
    /// Calls POST /services/eligdata/
    /// </summary>
    /// <param name="request">Eligibility inquiry parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed eligibility response including benefits data.</returns>
    Task<EligibilityResponse> CheckEligibilityAsync(
        EligibilityRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check eligibility by uploading a raw X12 270 file.
    /// Returns the X12 271 response as a string.
    /// Calls POST /services/elig/
    /// </summary>
    /// <param name="x12270Content">Raw X12 270 file content as a string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw X12 271 response string.</returns>
    Task<string> CheckEligibility270Async(
        string x12270Content,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────────────────────────
//  CLAIM SERVICE
// ─────────────────────────────────────────────

/// <summary>
/// Submits claims and retrieves claim status updates from Claim.MD.
/// </summary>
public interface IClaimService
{
    /// <summary>
    /// Upload a claim file (837P, 837I, JSON, XML, CSV, XLS, XLSX).
    /// Calls POST /services/upload/
    /// </summary>
    /// <param name="fileContent">File bytes to upload.</param>
    /// <param name="filename">Filename including extension (e.g. "claims.837").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ClaimUploadResponse> SubmitClaimFileAsync(
        byte[] fileContent,
        string filename,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a claim file from a stream.
    /// Calls POST /services/upload/
    /// </summary>
    Task<ClaimUploadResponse> SubmitClaimFileAsync(
        Stream fileStream,
        string filename,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Poll for claim status updates since the last known ResponseID.
    /// Calls POST /services/response/
    /// Store the returned LastResponseId and pass it on every subsequent call.
    /// First call should use responseId = "0".
    /// </summary>
    /// <param name="responseId">Last ResponseID received (use "0" initially).</param>
    /// <param name="claimMdId">
    /// Optional: filter to a specific Claim.MD claim ID.
    /// Do NOT use this parameter for periodic status polling.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ClaimStatusResponse> GetClaimStatusUpdatesAsync(
        string responseId,
        string? claimMdId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List files uploaded to Claim.MD.
    /// Calls POST /services/uploadlist/
    /// </summary>
    /// <param name="uploadDate">Optional: filter by upload date (yyyy-MM-dd).</param>
    /// <param name="page">Page number for pagination (500 results per page).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<UploadedFilesResponse> GetUploadedFilesAsync(
        string? uploadDate = null,
        int page = 1,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────────────────────────
//  ERA SERVICE
// ─────────────────────────────────────────────

/// <summary>
/// Retrieves Electronic Remittance Advice (ERA) from Claim.MD.
/// </summary>
public interface IEraService
{
    /// <summary>
    /// Get a list of available ERAs since the last known EraID.
    /// Calls POST /services/eralist/
    /// Store the returned LastEraId and pass it on every subsequent call.
    /// </summary>
    /// <param name="request">ERA list filter parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<EraListResponse> GetEraListAsync(
        EraListRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download full ERA detail in XML/JSON format.
    /// Calls POST /services/eradata/
    /// </summary>
    /// <param name="eraId">ERA ID obtained from GetEraListAsync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<EraDetailResponse> GetEraDetailAsync(
        string eraId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download ERA in raw X12 835 format.
    /// Calls POST /services/era835/
    /// </summary>
    /// <param name="eraId">ERA ID obtained from GetEraListAsync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw X12 835 string.</returns>
    Task<string> GetEra835Async(
        string eraId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download ERA as a PDF byte array.
    /// Calls POST /services/erapdf/
    /// </summary>
    /// <param name="eraId">ERA ID obtained from GetEraListAsync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF file bytes.</returns>
    Task<byte[]> GetEraPdfAsync(
        string eraId,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────────────────────────
//  PAYER SERVICE
// ─────────────────────────────────────────────

/// <summary>
/// Retrieves payer information from Claim.MD.
/// </summary>
public interface IPayerService
{
    /// <summary>
    /// Get the full list of supported payers.
    /// Calls POST /services/payerlist/
    /// </summary>
    /// <param name="payerId">Optional: filter to a specific payer by ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PayerListResponse> GetPayersAsync(
        string? payerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search payers by name (client-side filter over GetPayersAsync).
    /// </summary>
    Task<List<PayerInfo>> SearchPayersByNameAsync(
        string nameContains,
        CancellationToken cancellationToken = default);
}
