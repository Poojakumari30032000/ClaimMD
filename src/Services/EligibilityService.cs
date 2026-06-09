using System.Text;
using ClaimMD.Client.Configuration;
using ClaimMD.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClaimMD.Client.Services;

/// <inheritdoc cref="IEligibilityService"/>
internal sealed class EligibilityService : ClaimMdServiceBase, IEligibilityService
{
    public EligibilityService(
        HttpClient httpClient,
        IOptions<ClaimMdOptions> options,
        ILogger<EligibilityService> logger)
        : base(httpClient, options, logger) { }

    /// <inheritdoc/>
    public async Task<EligibilityResponse> CheckEligibilityAsync(
        EligibilityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateEligibilityRequest(request);

        var fields = BuildEligibilityFields(request);

        Logger.LogInformation(
            "Eligibility check: PayerID={PayerId} NPI={Npi} Ins={Last}/{First}",
            request.PayerId, request.ProviderNpi,
            request.InsuredLastName, request.InsuredFirstName);

        return await PostFormAsync<EligibilityResponse>(
            "services/eligdata/", fields, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string> CheckEligibility270Async(
        string x12270Content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(x12270Content))
            throw new ArgumentException("X12 270 content cannot be empty.", nameof(x12270Content));

        Logger.LogInformation("Eligibility 270 check (X12 passthrough)");

        var fileBytes = Encoding.UTF8.GetBytes(x12270Content);
        using var stream = new MemoryStream(fileBytes);

        // /services/elig/ returns a raw 271 string
        var fields = new Dictionary<string, string>();
        return await PostFormRawStringAsync("services/elig/", fields, cancellationToken);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static Dictionary<string, string> BuildEligibilityFields(EligibilityRequest req)
    {
        var fields = new Dictionary<string, string>
        {
            ["ins_last"]     = req.InsuredLastName,
            ["ins_first"]    = req.InsuredFirstName,
            ["ins_dob"]      = req.InsuredDateOfBirth,
            ["ins_number"]   = req.InsuredMemberId,
            ["prov_npi"]     = req.ProviderNpi,
            ["prov_taxid"]   = req.ProviderTaxId,
            ["payerid"]      = req.PayerId,
            ["dos"]          = req.DateOfService,
            ["pat_rel"]      = req.PatientRelationship,
            ["service_type"] = req.ServiceTypeCode,
        };

        if (req.InsuredGender is not null)         fields["ins_sex"]    = req.InsuredGender;
        if (req.PatientLastName is not null)        fields["pat_last"]   = req.PatientLastName;
        if (req.PatientFirstName is not null)       fields["pat_first"]  = req.PatientFirstName;
        if (req.PatientDateOfBirth is not null)     fields["pat_dob"]    = req.PatientDateOfBirth;
        if (req.PatientGender is not null)          fields["pat_sex"]    = req.PatientGender;
        if (req.ProviderLastOrOrgName is not null)  fields["prov_last"]  = req.ProviderLastOrOrgName;
        if (req.ProviderFirstName is not null)      fields["prov_first"] = req.ProviderFirstName;

        return fields;
    }

    private static void ValidateEligibilityRequest(EligibilityRequest req)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(req.InsuredLastName))   errors.Add("InsuredLastName is required.");
        if (string.IsNullOrWhiteSpace(req.InsuredFirstName))  errors.Add("InsuredFirstName is required.");
        if (string.IsNullOrWhiteSpace(req.InsuredDateOfBirth)) errors.Add("InsuredDateOfBirth is required (YYYYMMDD).");
        if (string.IsNullOrWhiteSpace(req.InsuredMemberId))   errors.Add("InsuredMemberId is required.");
        if (string.IsNullOrWhiteSpace(req.ProviderNpi))       errors.Add("ProviderNpi is required.");
        if (string.IsNullOrWhiteSpace(req.ProviderTaxId))     errors.Add("ProviderTaxId is required.");
        if (string.IsNullOrWhiteSpace(req.PayerId))           errors.Add("PayerId is required.");
        if (string.IsNullOrWhiteSpace(req.DateOfService))     errors.Add("DateOfService is required (YYYYMMDD).");

        if (req.InsuredDateOfBirth?.Length != 8 || !req.InsuredDateOfBirth.All(char.IsDigit))
            errors.Add("InsuredDateOfBirth must be in YYYYMMDD format.");

        if (req.DateOfService?.Length != 8 || !req.DateOfService.All(char.IsDigit))
            errors.Add("DateOfService must be in YYYYMMDD format.");

        if (!new[] { "18", "01", "19", "G8" }.Contains(req.PatientRelationship))
            errors.Add("PatientRelationship must be '18' (Self), '01' (Spouse), '19' (Child), or 'G8' (Other).");

        if (errors.Count > 0)
            throw new ArgumentException(string.Join(" ", errors));
    }
}
