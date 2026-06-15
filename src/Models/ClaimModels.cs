using System.Text.Json.Serialization;

namespace ClaimMD.Client.Models;

// ─────────────────────────────────────────────
//  CLAIM SUBMISSION  –  upload response
// ─────────────────────────────────────────────

/// <summary>
/// Response returned after uploading a claim file to /services/upload/.
/// </summary>
public sealed class ClaimUploadResponse
{
    [JsonPropertyName("messages")]
    public string? Messages { get; set; }

    [JsonPropertyName("claim")]          // was "claims"
    public List<ClaimAcknowledgment>? Claims { get; set; }

    [JsonPropertyName("error")]          // was "errors"
    public List<ApiError>? Errors { get; set; }

    [JsonIgnore]
    public bool IsSuccess => Errors == null || Errors.Count == 0;
}

/// <summary>
/// Acknowledgment for a single claim within an uploaded batch.
/// </summary>
public sealed class ClaimAcknowledgment
{
    /// <summary>Claim.MD internal claim ID.</summary>
    [JsonPropertyName("claimmd_id")]
    public string? ClaimMdId { get; set; }

    /// <summary>Your system's claim ID (from the 837 file).</summary>
    [JsonPropertyName("claimid")]
    public string? ClaimId { get; set; }

    /// <summary>Batch ID assigned by Claim.MD.</summary>
    [JsonPropertyName("batchid")]
    public string? BatchId { get; set; }

    /// <summary>Uploaded file ID.</summary>
    [JsonPropertyName("fileid")]
    public string? FileId { get; set; }

    /// <summary>Original filename.</summary>
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    /// <summary>Billing provider NPI.</summary>
    [JsonPropertyName("bill_npi")]
    public string? BillingNpi { get; set; }

    /// <summary>Billing provider Tax ID.</summary>
    [JsonPropertyName("bill_taxid")]
    public string? BillingTaxId { get; set; }

    /// <summary>Payer ID the claim was routed to.</summary>
    [JsonPropertyName("payerid")]
    public string? PayerId { get; set; }

    /// <summary>Insured member ID.</summary>
    [JsonPropertyName("ins_number")]
    public string? InsuredNumber { get; set; }

    /// <summary>First date of service (YYYY-MM-DD).</summary>
    [JsonPropertyName("fdos")]
    public string? FirstDateOfService { get; set; }

    /// <summary>Total charge amount.</summary>
    [JsonPropertyName("total_charge")]
    public string? TotalCharge { get; set; }

    /// <summary>Claim status: "A" = Acknowledged, "R" = Rejected.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Messages/acknowledgment details for this claim.</summary>
    [JsonPropertyName("messages")]
    public List<ClaimMessage>? ClaimMessages { get; set; }

    [JsonIgnore]
    public bool IsAcknowledged => Status == "A";
}

public sealed class ClaimMessage
{
    [JsonPropertyName("mesgid")]
    public string? MessageCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("fields")]
    public string? Fields { get; set; }

    [JsonPropertyName("responseid")]
    public string? ResponseId { get; set; }
}

// ─────────────────────────────────────────────
//  CLAIM STATUS  –  response polling
// ─────────────────────────────────────────────

/// <summary>
/// Result of polling /services/response/ for claim status updates.
/// </summary>
public sealed class ClaimStatusResponse
{
    [JsonPropertyName("last_responseid")]
    public string? LastResponseId { get; set; }

    [JsonPropertyName("claim")]          // was "claims"
    public List<ClaimStatusDetail>? Claims { get; set; }

    [JsonPropertyName("error")]          // was "errors"
    public List<ApiError>? Errors { get; set; }

    [JsonIgnore]
    public bool IsSuccess => Errors == null || Errors.Count == 0;
}

public sealed class ClaimStatusDetail
{
    [JsonPropertyName("claimmd_id")]
    public string? ClaimMdId { get; set; }

    [JsonPropertyName("claimid")]
    public string? ClaimId { get; set; }

    [JsonPropertyName("batchid")]
    public string? BatchId { get; set; }

    [JsonPropertyName("fileid")]
    public string? FileId { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("bill_npi")]
    public string? BillingNpi { get; set; }

    [JsonPropertyName("bill_taxid")]
    public string? BillingTaxId { get; set; }

    [JsonPropertyName("payerid")]
    public string? PayerId { get; set; }

    [JsonPropertyName("ins_number")]
    public string? InsuredNumber { get; set; }

    [JsonPropertyName("fdos")]
    public string? FirstDateOfService { get; set; }

    [JsonPropertyName("total_charge")]
    public string? TotalCharge { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("remote_claimid")]
    public string? RemoteClaimId { get; set; }

    [JsonPropertyName("pcn")]
    public string? Pcn { get; set; }

    [JsonPropertyName("sender_icn")]
    public string? SenderIcn { get; set; }

    [JsonPropertyName("sender_name")]
    public string? SenderName { get; set; }

    [JsonPropertyName("senderid")]
    public string? SenderId { get; set; }

    [JsonPropertyName("response_time")]
    public string? ResponseTime { get; set; }

    [JsonPropertyName("message")]
    public List<ClaimMessage>? Messages { get; set; }

    /// <summary>Human-readable status description.</summary>
    [JsonIgnore]
    public string StatusDescription => Status switch
    {
        "A" => "Acknowledged",
        "R" => "Rejected",
        "D" => "Denied",
        "P" => "Paid",
        "F" => "Forwarded",
        _ => Status ?? "Unknown"
    };
}

// ─────────────────────────────────────────────
//  UPLOADED FILES LIST
// ─────────────────────────────────────────────

public sealed class UploadedFilesResponse
{
    [JsonPropertyName("files")]
    public List<UploadedFileInfo>? Files { get; set; }

    [JsonPropertyName("error")]
    public List<ApiError>? Errors { get; set; }

    [JsonIgnore]
    public bool IsSuccess => Errors == null || Errors.Count == 0;
}

public sealed class UploadedFileInfo
{
    [JsonPropertyName("inboundid")]
    public string? InboundId { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("file_type")]
    public string? FileType { get; set; }

    [JsonPropertyName("file_count")]
    public string? FileCount { get; set; }

    [JsonPropertyName("file_amount")]
    public string? FileAmount { get; set; }

    [JsonPropertyName("uploadtime")]
    public string? UploadTime { get; set; }
}
