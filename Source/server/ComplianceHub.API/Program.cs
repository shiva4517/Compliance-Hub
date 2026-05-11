using ComplianceHub.API.Middleware;
using ComplianceHub.API.Services;
using ComplianceHub.Application;
using ComplianceHub.Infrastructure;
using ComplianceHub.Infrastructure.Data;
using ComplianceHub.Infrastructure.Data.Seeders;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Serilog — console + rolling file + Application Insights (when connection string is present)
var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/compliancehub-.log", rollingInterval: RollingInterval.Day);

if (!string.IsNullOrWhiteSpace(aiConnectionString))
{
    var telemetryConfig = TelemetryConfiguration.CreateDefault();
    telemetryConfig.ConnectionString = aiConnectionString;
    loggerConfig.WriteTo.ApplicationInsights(telemetryConfig, new TraceTelemetryConverter());
}

Log.Logger = loggerConfig.CreateLogger();
builder.Host.UseSerilog();

Log.Information("Compliance Hub API starting up. Application Insights sink active: {AiEnabled}", !string.IsNullOrWhiteSpace(aiConnectionString));

// Services
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Compliance Hub API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSingleton<SyncJobStore>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDataProtection();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddDbContextCheck<ComplianceHubDbContext>();

// CORS
var allowedOrigins = new List<string> { "http://localhost:5173", "http://localhost:3000" };
var clientUrl = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN_CLIENT_URL");
if (!string.IsNullOrWhiteSpace(clientUrl))
    allowedOrigins.Add(clientUrl);

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("AllowReact", policy =>
        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// Migrate and seed databases
using (var scope = app.Services.CreateScope())
{
    var complianceHubDb = scope.ServiceProvider.GetRequiredService<ComplianceHubDbContext>();
    await complianceHubDb.Database.MigrateAsync();
    await complianceHubDb.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "AiProviderConnections" (
            "Id" uuid NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "CreatedBy" text NULL,
            "UpdatedBy" text NULL,
            "IsDeleted" boolean NOT NULL,
            "SecurityUserId" uuid NOT NULL,
            "Provider" character varying(50) NOT NULL,
            "EncryptedApiKey" text NOT NULL,
            "Endpoint" character varying(500) NULL,
            "DeploymentName" character varying(200) NULL,
            "Model" character varying(200) NULL,
            "ApiVersion" character varying(50) NULL,
            CONSTRAINT "PK_AiProviderConnections" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_AiProviderConnections_SecurityUsers_SecurityUserId"
                FOREIGN KEY ("SecurityUserId") REFERENCES "SecurityUsers" ("Id") ON DELETE CASCADE
        );
        """);
    await complianceHubDb.Database.ExecuteSqlRawAsync("""
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_AiProviderConnections_SecurityUserId"
        ON "AiProviderConnections" ("SecurityUserId");
        """);

    var regulationsDb = scope.ServiceProvider.GetRequiredService<RegulationsDbContext>();
    await regulationsDb.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowReact");
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
