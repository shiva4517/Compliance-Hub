using System.Text;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Infrastructure.Data;
using ComplianceHub.Infrastructure.Data.Seeders;
using ComplianceHub.Infrastructure.ExternalApis.eCFR;
using ComplianceHub.Infrastructure.Repositories;
using ComplianceHub.Infrastructure.Services.Ai;
using ComplianceHub.Infrastructure.Mcp;
using ComplianceHub.Infrastructure.Services;
using ComplianceHub.Infrastructure.Services.Sync;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Polly;
using System.Net.Http.Headers;

namespace ComplianceHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // EF Core with PostgreSQL + Resilience via Polly
        services.AddDbContext<ComplianceHubDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null)));

        services.AddDbContext<RegulationsDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("RegulationsConnection"),
                npgsql => npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null)));

        // Repositories & UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRegulationsUnitOfWork, RegulationsUnitOfWork>();

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAiProviderSecretProtector, AiProviderSecretProtector>();
        services.AddScoped<IAiSuggestionService, SubscribedAiSuggestionService>();
        services.AddScoped<ILlmClientFactory, LlmClientFactory>();
        services.AddScoped<IAgentMcpService, AgentMcpService>();
        services.AddScoped<ISubscriptionMcpService, SubscriptionMcpService>();
        services.AddScoped<IAgentToolRouter, AgentToolRouter>();

        // Seeder
        services.AddScoped<DatabaseSeeder>();

        // In-Memory Caching
        services.AddMemoryCache();

        // JWT Authentication
        var jwtKey = config["Jwt:Key"]!;
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["Jwt:Issuer"],
                ValidAudience = config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy("SuperAdmin", p => p.RequireRole("SuperAdmin"))
            .AddPolicy("Admin", p => p.RequireRole("SuperAdmin", "Admin"))
            .AddPolicy("Customer", p => p.RequireRole("SuperAdmin", "Admin", "Customer"))
            .AddPolicy("Employee", p => p.RequireRole("SuperAdmin", "Admin", "Employee"));

        // eCFR HTTP client
        services.AddHttpClient("eCFR", c =>
        {
            c.BaseAddress = new Uri(config["RegulationSync:eCFRBaseUrl"] ?? "https://www.ecfr.gov");
            c.Timeout = TimeSpan.FromSeconds(90);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("ComplianceHub-Portal/1.0 (contact: jameer.sk@akeepo.com)");
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddHttpClient("AIProviders", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(90);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IECFRApiClient, ECFRApiClient>();
        services.AddScoped<IRegulationSyncOrchestrator, RegulationSyncOrchestrator>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();

        return services;
    }
}
