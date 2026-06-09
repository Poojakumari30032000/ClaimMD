# ClaimMD.Client

A production-ready .NET 8 library for integrating with the [Claim.MD](https://www.claim.md) clearinghouse API.

## Features

| Feature | Endpoint |
|---|---|
| **Real-Time Eligibility (JSON)** | `POST /services/eligdata/` |
| **Real-Time Eligibility (X12 270/271)** | `POST /services/elig/` |
| **Claim File Upload** (837P, 837I, JSON, XML, CSV, XLSX) | `POST /services/upload/` |
| **Claim Status Polling** | `POST /services/response/` |
| **Uploaded File List** | `POST /services/uploadlist/` |
| **ERA List** | `POST /services/eralist/` |
| **ERA Detail (JSON)** | `POST /services/eradata/` |
| **ERA Download (X12 835)** | `POST /services/era835/` |
| **ERA Download (PDF)** | `POST /services/erapdf/` |
| **Payer List / Search** | `POST /services/payerlist/` |

---

## Installation

Add the project reference or compile as a NuGet package:

```bash
dotnet add reference path/to/ClaimMD.Client.csproj
```

---

## Quick Start — Without DI

```csharp
using ClaimMD.Client;
using ClaimMD.Client.Models;

// Never hardcode your key — use environment variables or secrets management
var accountKey = Environment.GetEnvironmentVariable("CLAIMMD_ACCOUNT_KEY")!;

using var client = new ClaimMdClient(accountKey);

// ── Eligibility Check ────────────────────────────────────────────────
var eligResult = await client.CheckEligibilityAsync(new EligibilityRequest
{
    InsuredLastName     = "Smith",
    InsuredFirstName    = "Jane",
    InsuredDateOfBirth  = "19800315",      // YYYYMMDD
    InsuredMemberId     = "ABC123456789",
    PayerId             = "00431",         // Aetna — get from GetPayersAsync()
    ProviderNpi         = "1234567890",
    ProviderTaxId       = "123456789",
    DateOfService       = "20260610",
    PatientRelationship = "18",            // Self
    ServiceTypeCode     = "30",            // Health benefit plan
});

if (eligResult.IsSuccess)
{
    Console.WriteLine($"Subscriber: {eligResult.Subscriber?.FirstName} {eligResult.Subscriber?.LastName}");
    foreach (var benefit in eligResult.Benefits ?? [])
        Console.WriteLine($"  {benefit.BenefitDescription} — {benefit.Amount}");
}
else
{
    foreach (var err in eligResult.Errors ?? [])
        Console.Error.WriteLine(err);
}

// ── Submit a Claim File ──────────────────────────────────────────────
var claimBytes  = await File.ReadAllBytesAsync("professional_claims.837");
var uploadResult = await client.SubmitClaimAsync(claimBytes, "professional_claims.837");

if (uploadResult.IsSuccess)
{
    foreach (var ack in uploadResult.Claims ?? [])
        Console.WriteLine($"Claim {ack.ClaimId}: {(ack.IsAcknowledged ? "Accepted" : "Rejected")}");
}
```

---

## DI / ASP.NET Core Setup

**appsettings.json**

```json
{
  "ClaimMD": {
    "AccountKey": "",
    "BaseUrl": "https://svc.claim.md",
    "TimeoutSeconds": 30
  }
}
```

> **Security**: Store `AccountKey` in environment variables or Azure Key Vault, not in appsettings.json.

**Program.cs**

```csharp
using ClaimMD.Client.Extensions;

builder.Services.AddClaimMd(
    builder.Configuration.GetSection("ClaimMD"));

// Or configure inline:
builder.Services.AddClaimMd(opts =>
{
    opts.AccountKey = builder.Configuration["ClaimMD:AccountKey"]!;
});
```

**Inject and use**

```csharp
public class EligibilityController : ControllerBase
{
    private readonly IEligibilityService _eligibility;
    private readonly IClaimService _claims;

    public EligibilityController(IEligibilityService eligibility, IClaimService claims)
    {
        _eligibility = eligibility;
        _claims      = claims;
    }

    [HttpPost("check")]
    public async Task<IActionResult> Check([FromBody] EligibilityRequest req, CancellationToken ct)
    {
        var result = await _eligibility.CheckEligibilityAsync(req, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result.Errors);
    }
}
```

---

## Claim Status Polling Pattern

Claim.MD uses a cursor-based polling model. Store `last_responseid` and pass it on every call.

```csharp
// ── Persist this value in your database per account ──
string lastResponseId = await GetStoredResponseId(); // e.g. "0" on first run

var statusResult = await claimService.GetClaimStatusUpdatesAsync(lastResponseId);

if (statusResult.IsSuccess)
{
    foreach (var claim in statusResult.Claims ?? [])
    {
        Console.WriteLine($"ClaimMD ID: {claim.ClaimMdId}  Status: {claim.StatusDescription}");
        foreach (var msg in claim.Messages ?? [])
            Console.WriteLine($"  [{msg.MessageCode}] {msg.Message}");
    }

    // Always persist the new cursor after processing
    await SaveResponseId(statusResult.LastResponseId!);
}
```

---

## ERA Polling Pattern

Same cursor-based model using `last_eraid`.

```csharp
string lastEraId = await GetStoredEraId(); // "0" on first run

var eraList = await eraService.GetEraListAsync(new EraListRequest { EraId = lastEraId });

foreach (var era in eraList.Eras ?? [])
{
    // Download full detail
    var detail = await eraService.GetEraDetailAsync(era.EraId!);
    // Or download X12 835
    var raw835 = await eraService.GetEra835Async(era.EraId!);
    // Or download PDF
    var pdf    = await eraService.GetEraPdfAsync(era.EraId!);
    await File.WriteAllBytesAsync($"era_{era.EraId}.pdf", pdf);
}

await SaveEraId(eraList.LastEraId!);
```

---

## Payer Lookup

```csharp
// Get all payers
var allPayers = await payerService.GetPayersAsync();

// Filter to eligibility-supported payers
var eligPayers = allPayers.Payers?
    .Where(p => p.SupportsEligibility)
    .ToList();

// Search by name
var aetnaPayers = await payerService.SearchPayersByNameAsync("Aetna");
```

---

## X12 270/271 Eligibility (Passthrough)

```csharp
var x12_270 = await File.ReadAllTextAsync("eligibility_request.270");
var x12_271 = await eligibilityService.CheckEligibility270Async(x12_270);
await File.WriteAllTextAsync("eligibility_response.271", x12_271);
```

---

## Error Handling

```csharp
try
{
    var result = await client.CheckEligibilityAsync(request);
}
catch (ClaimMdApiException ex) when (ex.HttpStatusCode == 401)
{
    // Invalid or missing AccountKey
    logger.LogError("Authentication failed. Check your ClaimMD AccountKey.");
}
catch (ClaimMdApiException ex)
{
    foreach (var err in ex.Errors)
        logger.LogError("ClaimMD Error {Code}: {Message}", err.Code, err.Message);
}
catch (HttpRequestException ex)
{
    // Network / connectivity issue
    logger.LogError(ex, "Network error communicating with Claim.MD.");
}
catch (TaskCanceledException)
{
    // Timeout
    logger.LogWarning("Claim.MD request timed out.");
}
```

---

## Project Structure

```
ClaimMD.Client/
├── src/
│   ├── ClaimMD.Client.csproj
│   ├── ClaimMdClient.cs               ← Standalone client (no DI required)
│   ├── Configuration/
│   │   └── ClaimMdOptions.cs          ← API key, base URL, timeout
│   ├── Models/
│   │   ├── EligibilityModels.cs       ← Request + response DTOs
│   │   ├── ClaimModels.cs             ← Upload, status, file list DTOs
│   │   └── SharedModels.cs            ← ERA, payer, ApiError, exception
│   ├── Services/
│   │   ├── IClaimMdServices.cs        ← Interfaces (DI-friendly)
│   │   ├── ClaimMdServiceBase.cs      ← HTTP + auth + JSON parsing
│   │   ├── EligibilityService.cs
│   │   ├── ClaimService.cs
│   │   ├── EraService.cs
│   │   └── PayerService.cs
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs  ← AddClaimMd(...)
└── tests/
    ├── ClaimMD.Client.Tests.csproj
    └── EligibilityRequestValidationTests.cs
```

---

## API Rate Limits

Claim.MD enforces **100 requests per minute**. For high-volume scenarios, add Polly-based rate limiting:

```csharp
// In Program.cs — add after AddClaimMd(...)
services.AddHttpClient<IEligibilityService, EligibilityService>()
    .AddStandardResilienceHandler(opts =>
    {
        opts.RateLimiter.DefaultRateLimiterPolicy = ...;
        opts.Retry.MaxRetryAttempts = 2;
    });
```

---

## Security Notes

- Never commit your `AccountKey` to source control.
- Use `dotnet user-secrets`, environment variables, or Azure Key Vault.
- PHI (Protected Health Information) in transit is encrypted by HTTPS; ensure your logging doesn't capture it at the Trace level in production.
