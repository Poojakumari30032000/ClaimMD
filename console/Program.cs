using ClaimMD.Client;
using ClaimMD.Client.Models;

// ─────────────────────────────────────────────────────────
//  PASTE YOUR CLAIM.MD API KEY HERE
// ─────────────────────────────────────────────────────────
const string AccountKey = "YOUR_API_KEY_HERE";
// ─────────────────────────────────────────────────────────

if (AccountKey == "YOUR_API_KEY_HERE")
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Please set your AccountKey in Program.cs before running.");
    Console.ResetColor();
    return;
}

using var client = new ClaimMdClient(AccountKey);

await RunStep("1 — Payer List (connectivity + auth check)", async () =>
{
    var result = await client.Payers.GetPayersAsync();

    if (!result.IsSuccess)
    {
        PrintErrors(result.Errors);
        return;
    }

    var payers = result.Payers ?? [];
    Console.WriteLine($"  Total payers returned : {payers.Count}");
    Console.WriteLine($"  Eligibility-supported : {payers.Count(p => p.SupportsEligibility)}");
    Console.WriteLine($"  ERA-supported         : {payers.Count(p => p.SupportsEra)}");
    Console.WriteLine();
    Console.WriteLine("  Sample payers:");
    foreach (var p in payers.Take(5))
        Console.WriteLine($"    [{p.PayerId}] {p.PayerName}  elig={p.EligibilitySupported}  era={p.EraSupported}");
});

await RunStep("2 — Eligibility Check (JSON)", async () =>
{
    var request = new EligibilityRequest
    {
        // ── Edit these fields with real or test values ──────────────
        InsuredLastName    = "Smith",
        InsuredFirstName   = "Jane",
        InsuredDateOfBirth = "19800315",        // YYYYMMDD
        InsuredMemberId    = "TEST123456",
        PayerId            = "00431",           // Replace with a valid payer ID from step 1
        ProviderNpi        = "1234567890",
        ProviderTaxId      = "123456789",
        DateOfService      = DateTime.Today.ToString("yyyyMMdd"),
        PatientRelationship = "18",             // 18 = Self
        ServiceTypeCode    = "30",              // 30 = Health benefit plan
        // ───────────────────────────────────────────────────────────
    };

    var result = await client.CheckEligibilityAsync(request);

    if (!result.IsSuccess)
    {
        PrintErrors(result.Errors);
        return;
    }

    Console.WriteLine($"  Subscriber : {result.Subscriber?.FirstName} {result.Subscriber?.LastName}");
    Console.WriteLine($"  Member ID  : {result.Subscriber?.MemberId}");
    Console.WriteLine($"  Payer      : {result.Payer?.Name} ({result.Payer?.PayerId})");
    Console.WriteLine();

    var benefits = result.Benefits ?? [];
    Console.WriteLine($"  Benefits returned: {benefits.Count}");
    foreach (var b in benefits.Take(5))
        Console.WriteLine($"    {b.BenefitDescription,-30} Amount={b.Amount,-10} Network={b.InNetwork}");
});

await RunStep("3 — Claim Status Poll", async () =>
{
    // Use "0" on first run — stores last_responseid for incremental polling
    var result = await client.Claims.GetClaimStatusUpdatesAsync("0");

    if (!result.IsSuccess)
    {
        PrintErrors(result.Errors);
        return;
    }

    Console.WriteLine($"  Last Response ID : {result.LastResponseId}");
    Console.WriteLine($"  Claims returned  : {result.Claims?.Count ?? 0}");
    Console.WriteLine();

    foreach (var claim in (result.Claims ?? []).Take(5))
    {
        Console.WriteLine($"  ClaimMD ID : {claim.ClaimMdId}  Status : {claim.StatusDescription}");
        foreach (var msg in claim.Messages ?? [])
            Console.WriteLine($"    [{msg.MessageCode}] {msg.Message}");
    }

    if (result.Claims?.Count == 0)
        Console.WriteLine("  No claim updates yet — this is normal for a new account.");
});

await RunStep("4 — ERA List", async () =>
{
    var result = await client.Era.GetEraListAsync(new EraListRequest { EraId = "0" });

    if (!result.IsSuccess)
    {
        PrintErrors(result.Errors);
        return;
    }

    var eras = result.Eras ?? [];
    Console.WriteLine($"  Last ERA ID    : {result.LastEraId}");
    Console.WriteLine($"  ERAs returned  : {eras.Count}");

    foreach (var era in eras.Take(5))
        Console.WriteLine($"  ERA {era.EraId,-10} Payer={era.PayerName,-25} Amount={era.CheckAmount}");

    if (eras.Count == 0)
        Console.WriteLine("  No ERAs yet — this is normal for a new or test account.");
});

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("All steps complete.");
Console.ResetColor();

// ─────────────────────────────────────────────────────────
//  Helpers
// ─────────────────────────────────────────────────────────

static async Task RunStep(string title, Func<Task> action)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"=== Step {title} ===");
    Console.ResetColor();

    try
    {
        await action();
    }
    catch (ClaimMdApiException ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  API error: {ex.Message}");
        if (ex.HttpStatusCode.HasValue)
            Console.WriteLine($"  HTTP status: {ex.HttpStatusCode}");
        Console.ResetColor();
    }
    catch (HttpRequestException ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  Network error: {ex.Message}");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  Unexpected error: {ex.Message}");
        Console.ResetColor();
    }
}

static void PrintErrors(List<ApiError>? errors)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    foreach (var e in errors ?? [])
        Console.WriteLine($"  {e}");
    Console.ResetColor();
}
