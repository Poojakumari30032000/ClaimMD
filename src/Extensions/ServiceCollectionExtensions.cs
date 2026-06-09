using ClaimMD.Client.Configuration;
using ClaimMD.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClaimMD.Client.Extensions;

/// <summary>
/// Extension methods for registering Claim.MD services with Microsoft DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Claim.MD services with the DI container.
    /// </summary>
    /// <example>
    /// In Program.cs / Startup.cs:
    /// <code>
    /// builder.Services.AddClaimMd(options =>
    /// {
    ///     options.AccountKey = builder.Configuration["ClaimMD:AccountKey"]!;
    /// });
    /// </code>
    /// Or bind from appsettings.json "ClaimMD" section:
    /// <code>
    /// builder.Services.AddClaimMd(
    ///     builder.Configuration.GetSection(ClaimMdOptions.SectionName));
    /// </code>
    /// </example>
    public static IServiceCollection AddClaimMd(
        this IServiceCollection services,
        Action<ClaimMdOptions> configure)
    {
        services.Configure(configure);
        RegisterServices(services);
        return services;
    }

    /// <summary>
    /// Registers Claim.MD services, binding options from an IConfiguration section.
    /// </summary>
    public static IServiceCollection AddClaimMd(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configSection)
    {
        services.Configure<ClaimMdOptions>(configSection);
        RegisterServices(services);
        return services;
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // Typed HttpClients — one per service, isolated connection pools
        services.AddHttpClient<IEligibilityService, EligibilityService>()
            .ConfigureHttpClient(ConfigureClient);

        services.AddHttpClient<IClaimService, ClaimService>()
            .ConfigureHttpClient(ConfigureClient);

        services.AddHttpClient<IEraService, EraService>()
            .ConfigureHttpClient(ConfigureClient);

        services.AddHttpClient<IPayerService, PayerService>()
            .ConfigureHttpClient(ConfigureClient);
    }

    private static void ConfigureClient(IServiceProvider sp, HttpClient client)
    {
        var opts = sp.GetRequiredService<IOptions<ClaimMdOptions>>().Value;
        client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
        client.Timeout     = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    }
}
