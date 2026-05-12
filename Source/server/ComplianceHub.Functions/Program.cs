using ComplianceHub.Functions.Data;
using ComplianceHub.Functions.Options;
using ComplianceHub.Functions.Services;
using ComplianceHub.Functions.Telemetry;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

// Dapper needs explicit handlers for DateOnly/DateOnly?; register once at startup
// so every Dapper call (sync, outbox, etc.) handles DateOnly params correctly.
ComplianceHub.Functions.Services.DapperConfig.RegisterTypeHandlers();

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;

        // Application Insights for isolated worker
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // ── eCFR HTTP client ────────────────────────────────────────────────────
        services.AddHttpClient("eCFR", c =>
        {
            c.BaseAddress = new Uri(cfg["EcfrApi:BaseUrl"]!);
            c.Timeout = TimeSpan.FromSeconds(90);
        });

        // ── Dapper data sources (regulations sync) ──────────────────────────────
        services.AddSingleton(_ => NpgsqlDataSource.Create(cfg["ConnectionStrings:RegulationsConnection"]!));
        services.AddKeyedSingleton<NpgsqlDataSource>("compliancehub", (_, _) =>
            NpgsqlDataSource.Create(cfg["ConnectionStrings:DefaultConnection"]!));

        // ── EF Core DbContext for notification pipeline (portal DB) ─────────────
        services.AddDbContext<NotificationDbContext>(opts =>
            opts.UseNpgsql(cfg["ConnectionStrings:DefaultConnection"]!),
            ServiceLifetime.Scoped);

        // ── Azure Service Bus (notification outbox publisher) ───────────────────
        services.AddSingleton(sp =>
            new ServiceBusClient(cfg["ServiceBusConnection"]!));
        services.AddSingleton(sp =>
            sp.GetRequiredService<ServiceBusClient>()
              .CreateSender(cfg["ServiceBusQueueName"]!));

        // ── Strongly-typed options ──────────────────────────────────────────────
        services.Configure<EmailOptions>(cfg.GetSection("Email"));
        services.Configure<RetryOptions>(cfg.GetSection("Retry"));
        services.Configure<OutboxOptions>(cfg.GetSection("Outbox"));

        // ── Telemetry wrapper ───────────────────────────────────────────────────
        services.AddSingleton<AppInsightsTelemetry>();

        // ── Regulation sync services ────────────────────────────────────────────
        services.AddScoped<EcfrClient>();
        services.AddScoped<RegulationSyncService>();
        services.AddScoped<OutboxProcessorService>();

        // ── Notification pipeline services ──────────────────────────────────────
        services.AddScoped<EmailTemplateService>();
        services.AddScoped<EmailSenderService>();
    })
    .ConfigureLogging(logging =>
    {
        // The Functions host registers a default rule that drops AI logs below Warning.
        // Remove it so ILogger.LogInformation calls flow through to Application Insights.
        logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            var defaultRule = options.Rules.FirstOrDefault(r =>
                r.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
            if (defaultRule is not null)
                options.Rules.Remove(defaultRule);
        });
    })
    .Build();

await host.RunAsync();
