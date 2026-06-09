using System.Text.Json.Serialization;

namespace ClaimMD.Client.Models;

// ─────────────────────────────────────────────
//  ERA  –  Electronic Remittance Advice
// ─────────────────────────────────────────────

/// <summary>
/// Request parameters for /services/eralist/.
/// </summary>
public sealed class EraListRequest
{
    /// <summary>
    /// Return only ERAs with an ID greater than this value.
    /// Store the last returned EraId and pass it on subsequent calls.
    /// Use "0" for the first call.
    /// </summary>
    public string EraId { get; set; } = "0";

    /// <summary>Optional: filter by payer ID.</summary>
    public string? PayerId { get; set; }
}

/// <summary>
/// List of ERA headers returned by /services/eralist/.
/// </summary>
public sealed class EraListResponse
{
    /// <summary>
    /// Store this value and pass as EraId on the next poll to
    /// receive only new ERAs since this call.
    /// </summary>
    [JsonPropertyName("last_eraid")]
    public string? LastEraId { get; set; }

    [JsonPropertyName("eras")]
    public List<EraHeader>? Eras { get; set; }

    [JsonPropertyName("errors")]
    public List<ApiError>? Errors { get; set; }

    [JsonIgnore]
    public bool IsSuccess => Errors == null || Errors.Count == 0;
}

public sealed class EraHeader
{
    [JsonPropertyName("eraid")]
    public string? EraId { get; set; }

    [JsonPropertyName("payerid")]
    public string? PayerId { get; set; }

    [JsonPropertyName("payer_name")]
    public string? PayerName { get; set; }

    [JsonPropertyName("check_date")]
    public string? CheckDate { get; set; }

    [JsonPropertyName("check_number")]
    public string? CheckNumber { get; set; }

    [JsonPropertyName("check_amount")]
    public string? CheckAmount { get; set; }

    [JsonPropertyName("npi")]
    public string? Npi { get; set; }

    [JsonPropertyName("taxid")]
    public string? TaxId { get; set; }
}

/// <summary>
/// Full ERA detail in XML/JSON format from /services/eradata/.
/// </summary>
public sealed class EraDetailResponse
{
    [JsonPropertyName("eraid")]
    public string? EraId { get; set; }

    [JsonPropertyName("payerid")]
    public string? PayerId { get; set; }

    [JsonPropertyName("payer_name")]
    public string? PayerName { get; set; }

    [JsonPropertyName("check_date")]
    public string? CheckDate { get; set; }

    [JsonPropertyName("check_number")]
    public string? CheckNumber { get; set; }

    [JsonPropertyName("check_amount")]
    public string? CheckAmount { get; set; }

    [JsonPropertyName("claims")]
    public List<EraClaimDetail>? Claims { get; set; }

    [JsonPropertyName("errors")]
    public List<ApiError>? Errors { get; set; }

    [JsonIgnore]
    public bool IsSuccess => Errors == null || Errors.Count == 0;
}

public sealed class EraClaimDetail
{
    [JsonPropertyName("claimmd_id")]
    public string? ClaimMdId { get; set; }

    [JsonPropertyName("claimid")]
    public string? ClaimId { get; set; }

    [JsonPropertyName("patient_name")]
    public string? PatientName { get; set; }

    [JsonPropertyName("patient_id")]
    public string? PatientId { get; set; }

    [JsonPropertyName("dos")]
    public string? DateOfService { get; set; }

    [JsonPropertyName("billed")]
    public string? Billed { get; set; }

    [JsonPropertyName("paid")]
    public string? Paid { get; set; }

    [JsonPropertyName("adjustment")]
    public string? Adjustment { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("service_lines")]
    public List<EraServiceLine>? ServiceLines { get; set; }
}

public sealed class EraServiceLine
{
    [JsonPropertyName("proc_code")]
    public string? ProcedureCode { get; set; }

    [JsonPropertyName("mod")]
    public string? Modifier { get; set; }

    [JsonPropertyName("dos")]
    public string? DateOfService { get; set; }

    [JsonPropertyName("billed")]
    public string? Billed { get; set; }

    [JsonPropertyName("paid")]
    public string? Paid { get; set; }

    [JsonPropertyName("adjustment_reason")]
    public string? AdjustmentReason { get; set; }
}

// ─────────────────────────────────────────────
//  PAYER LIST
// ─────────────────────────────────────────────

public sealed class PayerListResponse
{
    [JsonPropertyName("payers")]
    public List<PayerInfo>? Payers { get; set; }

    [JsonPropertyName("errors")]
    public List<ApiError>? Errors { get; set; }

    [JsonIgnore]
    public bool IsSuccess => Errors == null || Errors.Count == 0;
}

public sealed class PayerInfo
{
    [JsonPropertyName("payerid")]
    public string? PayerId { get; set; }

    [JsonPropertyName("payer_name")]
    public string? PayerName { get; set; }

    [JsonPropertyName("claim_types")]
    public string? ClaimTypes { get; set; }

    [JsonPropertyName("elig_supported")]
    public string? EligibilitySupported { get; set; }

    [JsonPropertyName("era_supported")]
    public string? EraSupported { get; set; }

    [JsonIgnore]
    public bool SupportsEligibility =>
        string.Equals(EligibilitySupported, "Y", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool SupportsEra =>
        string.Equals(EraSupported, "Y", StringComparison.OrdinalIgnoreCase);
}

// ─────────────────────────────────────────────
//  SHARED / COMMON
// ─────────────────────────────────────────────

/// <summary>
/// An API-level error returned by Claim.MD.
/// </summary>
public sealed class ApiError
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("field")]
    public string? Field { get; set; }

    public override string ToString() =>
        Field != null
            ? $"[{Code}] {Field}: {Message}"
            : $"[{Code}] {Message}";
}

/// <summary>
/// Exception thrown when the Claim.MD API returns one or more errors.
/// </summary>
public sealed class ClaimMdApiException : Exception
{
    public IReadOnlyList<ApiError> Errors { get; }
    public int? HttpStatusCode { get; }

    public ClaimMdApiException(IEnumerable<ApiError> errors, int? httpStatusCode = null)
        : base(BuildMessage(errors))
    {
        Errors = errors.ToList().AsReadOnly();
        HttpStatusCode = httpStatusCode;
    }

    public ClaimMdApiException(string message, Exception? inner = null)
        : base(message, inner)
    {
        Errors = Array.Empty<ApiError>();
    }

    private static string BuildMessage(IEnumerable<ApiError> errors) =>
        string.Join("; ", errors.Select(e => e.ToString()));
}
