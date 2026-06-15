using System.Text.Json.Serialization;

namespace ClaimMD.Client.Models;

// ─────────────────────────────────────────────
//  ELIGIBILITY  –  JSON / structured request
// ─────────────────────────────────────────────

/// <summary>
/// Parameters for a real-time eligibility inquiry submitted as JSON.
/// Maps directly to the Claim.MD /services/eligdata/ endpoint fields.
/// </summary>
public sealed class EligibilityRequest
{
    // ── Insured / subscriber ──────────────────────────────────────────
    /// <summary>Insured last name (required).</summary>
    [JsonPropertyName("ins_last")]
    public string InsuredLastName { get; set; } = string.Empty;

    /// <summary>Insured first name (required).</summary>
    [JsonPropertyName("ins_first")]
    public string InsuredFirstName { get; set; } = string.Empty;

    /// <summary>Insured date of birth. Format: YYYYMMDD (required).</summary>
    [JsonPropertyName("ins_dob")]
    public string InsuredDateOfBirth { get; set; } = string.Empty;

    /// <summary>Insurance ID / member number (required).</summary>
    [JsonPropertyName("ins_number")]
    public string InsuredMemberId { get; set; } = string.Empty;

    /// <summary>Insured gender: M or F.</summary>
    [JsonPropertyName("ins_sex")]
    public string? InsuredGender { get; set; }

    // ── Patient ───────────────────────────────────────────────────────
    /// <summary>
    /// Patient relationship to insured.
    /// "18" = Self, "01" = Spouse, "19" = Child, "G8" = Other.
    /// Defaults to "18" (self).
    /// </summary>
    [JsonPropertyName("pat_rel")]
    public string PatientRelationship { get; set; } = "18";

    /// <summary>Patient last name (required if patient ≠ insured).</summary>
    [JsonPropertyName("pat_last")]
    public string? PatientLastName { get; set; }

    /// <summary>Patient first name (required if patient ≠ insured).</summary>
    [JsonPropertyName("pat_first")]
    public string? PatientFirstName { get; set; }

    /// <summary>Patient date of birth. Format: YYYYMMDD.</summary>
    [JsonPropertyName("pat_dob")]
    public string? PatientDateOfBirth { get; set; }

    /// <summary>Patient gender: M or F.</summary>
    [JsonPropertyName("pat_sex")]
    public string? PatientGender { get; set; }

    // ── Provider ─────────────────────────────────────────────────────
    /// <summary>Billing provider NPI (required).</summary>
    [JsonPropertyName("prov_npi")]
    public string ProviderNpi { get; set; } = string.Empty;

    /// <summary>Billing provider Tax ID / EIN (required).</summary>
    [JsonPropertyName("prov_taxid")]
    public string ProviderTaxId { get; set; } = string.Empty;

    /// <summary>Billing provider last/org name.</summary>
    [JsonPropertyName("prov_last")]
    public string? ProviderLastOrOrgName { get; set; }

    /// <summary>Billing provider first name.</summary>
    [JsonPropertyName("prov_first")]
    public string? ProviderFirstName { get; set; }

    // ── Payer ─────────────────────────────────────────────────────────
    /// <summary>Claim.MD payer ID (required). Use /services/payerlist/ to look up.</summary>
    [JsonPropertyName("payerid")]
    public string PayerId { get; set; } = string.Empty;

    // ── Service ───────────────────────────────────────────────────────
    /// <summary>Date of service. Format: YYYYMMDD (required).</summary>
    [JsonPropertyName("dos")]
    public string DateOfService { get; set; } = string.Empty;

    /// <summary>
    /// Service type code. "30" = Health Benefit Plan Coverage (default).
    /// See X12 271 service type codes for full list.
    /// </summary>
    [JsonPropertyName("service_type")]
    public string ServiceTypeCode { get; set; } = "30";
}

// ─────────────────────────────────────────────
//  ELIGIBILITY  –  response
// ─────────────────────────────────────────────

/// <summary>
/// Top-level eligibility response returned by Claim.MD.
/// </summary>
public sealed class EligibilityResponse
{
    [JsonPropertyName("error")]
    public List<ApiError>? Errors { get; set; }

    [JsonPropertyName("subscriber")]
    public List<EligibilitySubscriber>? SubscriberList { get; set; }

    [JsonPropertyName("payer")]
    public List<EligibilityPayer>? PayerList { get; set; }

    [JsonPropertyName("benefit")]
    public List<EligibilityBenefit>? Benefits { get; set; }

    [JsonIgnore]
    public EligibilitySubscriber? Subscriber => SubscriberList?.FirstOrDefault();

    [JsonIgnore]
    public EligibilityPayer? Payer => PayerList?.FirstOrDefault();

    [JsonIgnore]
    public bool IsSuccess => Errors == null || Errors.Count == 0;
}

public sealed class EligibilitySubscriber
{
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("member_id")]
    public string? MemberId { get; set; }

    [JsonPropertyName("dob")]
    public string? DateOfBirth { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }
}

public sealed class EligibilityPayer
{
    [JsonPropertyName("payer_name")]
    public string? Name { get; set; }

    [JsonPropertyName("payer_id")]
    public string? PayerId { get; set; }
}

public sealed class EligibilityBenefit
{
    [JsonPropertyName("benefit_code")]
    public string? BenefitCode { get; set; }

    [JsonPropertyName("benefit_desc")]
    public string? BenefitDescription { get; set; }

    [JsonPropertyName("coverage_level")]
    public string? CoverageLevel { get; set; }

    [JsonPropertyName("insurance_type")]
    public string? InsuranceType { get; set; }

    [JsonPropertyName("time_period")]
    public string? TimePeriod { get; set; }

    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    [JsonPropertyName("percent")]
    public string? Percent { get; set; }

    [JsonPropertyName("in_plan_network")]
    public string? InNetwork { get; set; }

    [JsonPropertyName("message")]
    public List<string>? Messages { get; set; }
}
