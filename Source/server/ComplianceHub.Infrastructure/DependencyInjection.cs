using System.Net.Http.Headers;
using System.Text;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;
using ComplianceHub.Infrastructure.Data;
using ComplianceHub.Infrastructure.Data.Seeders;
using ComplianceHub.Infrastructure.ExternalApis.eCFR;
using ComplianceHub.Infrastructure.Mcp;
using ComplianceHub.Infrastructure.Repositories;
using ComplianceHub.Infrastructure.Services;
using ComplianceHub.Infrastructure.Services.Ai;
using ComplianceHub.Infrastructure.Services.AiPrAutomation.Agents;
using ComplianceHub.Infrastructure.Services.AiPrAutomation.Ai;
using ComplianceHub.Infrastructure.Services.AiPrAutomation.Execution;
using ComplianceHub.Infrastructure.Services.AiPrAutomation.Git;
using ComplianceHub.Infrastructure.Services.AiPrAutomation.Validation;
using ComplianceHub.Infrastructure.Services.AiPrAutomation.Workspace;
using ComplianceHub.Infrastructure.Services.AiPrAutomation.Workflow;
using ComplianceHub.Infrastructure.Services.Sync;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ComplianceHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiPrAutomationInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AiPrAutomationOptions>(config.GetSection(AiPrAutomationOptions.SectionName));
        services.AddSingleton<ProcessCommandRunner>();
        services.AddScoped<IGitProviderFactory, GitProviderFactory>();
        services.AddScoped<IGitProvider, GitHubGitProvider>();
        services.AddScoped<IGitProvider, AzureDevOpsGitProvider>();
        services.AddScoped<IGitProvider, GitLabGitProvider>();
        services.AddScoped<IGitProvider, BitbucketGitProvider>();
        services.AddScoped<IGitProvider, SelfHostedGitProvider>();
        services.AddScoped<IAiPrModelProviderFactory, AiPrModelProviderFactory>();
        services.AddScoped<IAiPrModelProvider, MockAiPrModelProvider>();
        services.AddScoped<IAiPrModelProvider, AnthropicAiPrModelProvider>();
        services.AddScoped<IAiPrModelProvider, OpenAiAiPrModelProvider>();
        services.AddScoped<IAiPrModelProvider, GeminiAiPrModelProvider>();
        services.AddScoped<ICodeGenerationAgent, CodeGenerationAgent>();
        services.AddScoped<IPullRequestReviewerAgent, PullRequestReviewerAgent>();
        services.AddScoped<IReviewFixAgent, ReviewFixAgent>();
        services.AddScoped<IRepositoryWorkspaceService, RepositoryWorkspaceService>();
        services.AddScoped<IValidationRunner, ValidationRunner>();
        services.AddScoped<IAiPrWorkflowEngine, AiPrWorkflowEngine>();
        services.AddScoped<IAiPrEventOrchestrator, AiPrEventOrchestrator>();

        services.AddHttpClient("AiPrGitHub", c =>
        {
            c.BaseAddress = new Uri(config["AiPrAutomation:GitHub:ApiBaseUrl"] ?? "https://api.github.com/");
            c.Timeout = TimeSpan.FromSeconds(90);
        });

        services.AddHttpClient("AiPrAnthropic", c =>
        {
            c.BaseAddress = new Uri(config["AiPrAutomation:Anthropic:BaseUrl"] ?? "https://api.anthropic.com/v1/");
            c.Timeout = TimeSpan.FromSeconds(120);
        });

        services.AddHttpClient("AiPrOpenAi", c =>
        {
            c.BaseAddress = new Uri(config["AiPrAutomation:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/");
            c.Timeout = TimeSpan.FromSeconds(120);
        });

        services.AddHttpClient("AiPrGemini", c =>
        {
            c.BaseAddress = new Uri(config["AiPrAutomation:Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/openai/");
            c.Timeout = TimeSpan.FromSeconds(120);
        });

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
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

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRegulationsUnitOfWork, RegulationsUnitOfWork>();

        services.AddScoped<IJwtService, JwtService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAiProviderSecretProtector, AiProviderSecretProtector>();
        services.AddScoped<IAiSuggestionService, SubscribedAiSuggestionService>();
        services.AddScoped<ILlmClientFactory, LlmClientFactory>();
        services.AddScoped<IAgentMcpService, AgentMcpService>();
        services.AddScoped<ISubscriptionMcpService, SubscriptionMcpService>();
        services.AddScoped<IAgentToolRouter, AgentToolRouter>();
        services.AddAiPrAutomationInfrastructure(config);

        services.AddScoped<DatabaseSeeder>();
        services.AddMemoryCache();

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
