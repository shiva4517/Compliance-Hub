using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Agent.Models;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Infrastructure.Mcp;

public class AgentMcpService(ComplianceHubDbContext db) : IAgentMcpService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // ── Tool definitions ─────────────────────────────────────────────────────

    public IReadOnlyList<LlmToolDefinition> GetToolDefinitions() =>
    [
        new("create_company", "Create a new company. Collect all required fields, validate, preview, then confirm before calling this tool.",
            new
            {
                type = "object",
                properties = new
                {
                    companyName       = new { type = "string", description = "Company name (max 200 chars)" },
                    primaryEmail      = new { type = "string", description = "Primary email address (unique, max 200 chars)" },
                    secondaryEmail    = new { type = "string", description = "Secondary email (optional, max 200 chars)" },
                    phoneNumber       = new { type = "string", description = "Phone number (optional, max 20 chars)" },
                    primaryAddress    = new { type = "string", description = "Street address (max 300 chars)" },
                    primaryCity       = new { type = "string", description = "City (max 100 chars)" },
                    primaryState      = new { type = "string", description = "State (max 50 chars)" },
                    primaryPostalCode = new { type = "string", description = "Postal code (max 10 chars)" },
                    secondaryAddress  = new { type = "string", description = "Secondary street address (optional)" },
                    secondaryCity     = new { type = "string", description = "Secondary city (optional)" },
                    secondaryState    = new { type = "string", description = "Secondary state (optional)" },
                    secondaryPostalCode = new { type = "string", description = "Secondary postal code (optional)" },
                    websiteUrl        = new { type = "string", description = "Website URL (optional, must start with http/https)" }
                },
                required = new[] { "companyName", "primaryEmail", "primaryAddress", "primaryCity", "primaryState", "primaryPostalCode" }
            }),

        new("update_company", "Update an existing company. Only provided fields are updated — omitted fields are unchanged.",
            new
            {
                type = "object",
                properties = new
                {
                    companyId         = new { type = "string", description = "UUID of the company to update" },
                    companyName       = new { type = "string" },
                    primaryEmail      = new { type = "string" },
                    secondaryEmail    = new { type = "string" },
                    phoneNumber       = new { type = "string" },
                    primaryAddress    = new { type = "string" },
                    primaryCity       = new { type = "string" },
                    primaryState      = new { type = "string" },
                    primaryPostalCode = new { type = "string" },
                    secondaryAddress  = new { type = "string" },
                    secondaryCity     = new { type = "string" },
                    secondaryState    = new { type = "string" },
                    secondaryPostalCode = new { type = "string" },
                    websiteUrl        = new { type = "string" }
                },
                required = new[] { "companyId" }
            }),

        new("list_companies", "List companies with optional filters. Returns summaries sorted by company code.",
            new
            {
                type = "object",
                properties = new
                {
                    search    = new { type = "string", description = "Free-text search across name, email, city, state, code" },
                    city      = new { type = "string", description = "Filter by city (case-insensitive contains)" },
                    state     = new { type = "string", description = "Filter by state (case-insensitive contains)" },
                    isActive  = new { type = "boolean", description = "Filter by active status" },
                    pageSize  = new { type = "integer", description = "Max results (1-100, default 20)" }
                }
            }),

        new("get_company", "Get full details of a single company by ID or name.",
            new
            {
                type = "object",
                properties = new
                {
                    companyId   = new { type = "string", description = "UUID of the company" },
                    companyName = new { type = "string", description = "Exact or partial company name to search" }
                }
            }),

        new("delete_company", "Soft-delete a company by ID. Requires explicit user confirmation before calling.",
            new
            {
                type = "object",
                properties = new
                {
                    companyId = new { type = "string", description = "UUID of the company to delete" }
                },
                required = new[] { "companyId" }
            }),

        new("validate_company_data", "Validate company field values before create or update. Returns missing required fields and format errors.",
            new
            {
                type = "object",
                properties = new
                {
                    operation = new { type = "string", @enum = new[] { "create", "update" } },
                    fields    = new { type = "object", description = "Key-value map of company fields to validate" }
                },
                required = new[] { "operation", "fields" }
            }),

        new("preview_company_data", "Build a structured preview card for the collected company data. Show this to the user before any write operation.",
            new
            {
                type = "object",
                properties = new
                {
                    operation  = new { type = "string", @enum = new[] { "create", "update" } },
                    fields     = new { type = "object", description = "Collected company fields" },
                    companyId  = new { type = "string", description = "Required for update — existing company ID" }
                },
                required = new[] { "operation", "fields" }
            })
    ];

    // ── Dispatcher ───────────────────────────────────────────────────────────

    public async Task<string> InvokeToolAsync(string toolName, string argumentsJson, CancellationToken ct)
    {
        var args = JsonNode.Parse(argumentsJson) as JsonObject
                   ?? throw new ArgumentException("Tool arguments must be a JSON object.");

        return toolName switch
        {
            "create_company"      => await CreateCompanyAsync(args, ct),
            "update_company"      => await UpdateCompanyAsync(args, ct),
            "list_companies"      => await ListCompaniesAsync(args, ct),
            "get_company"         => await GetCompanyAsync(args, ct),
            "delete_company"      => await DeleteCompanyAsync(args, ct),
            "validate_company_data" => ValidateCompanyData(args),
            "preview_company_data"  => await PreviewCompanyDataAsync(args, ct),
            _ => Fail($"Unknown tool: {toolName}")
        };
    }

    // ── create_company ───────────────────────────────────────────────────────

    private async Task<string> CreateCompanyAsync(JsonObject args, CancellationToken ct)
    {
        var name         = Str(args, "companyName");
        var email        = Str(args, "primaryEmail");
        var address      = Str(args, "primaryAddress");
        var city         = Str(args, "primaryCity");
        var state        = Str(args, "primaryState");
        var postalCode   = Str(args, "primaryPostalCode");

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(city) ||
            string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(postalCode))
            return Fail("Missing required fields: companyName, primaryEmail, primaryAddress, primaryCity, primaryState, primaryPostalCode.");

        var emailLower = email.Trim().ToLower();
        var exists = await db.Companies
            .IgnoreQueryFilters()
            .AnyAsync(c => c.PrimaryEmail.ToLower() == emailLower, ct);

        if (exists)
            return Fail($"A company with primary email '{email}' already exists.");

        var code = await GenerateCompanyCodeAsync(ct);

        var company = new Company
        {
            CompanyCode       = code,
            CompanyName       = Trim(name),
            PrimaryEmail      = Trim(email),
            SecondaryEmail    = NullTrim(args, "secondaryEmail"),
            PhoneNumber       = NullTrim(args, "phoneNumber"),
            PrimaryAddress    = Trim(address),
            PrimaryCity       = Trim(city),
            PrimaryState      = Trim(state),
            PrimaryPostalCode = Trim(postalCode),
            SecondaryAddress  = NullTrim(args, "secondaryAddress"),
            SecondaryCity     = NullTrim(args, "secondaryCity"),
            SecondaryState    = NullTrim(args, "secondaryState"),
            SecondaryPostalCode = NullTrim(args, "secondaryPostalCode"),
            WebsiteUrl        = NullTrim(args, "websiteUrl"),
            IsActive          = true,
            CreatedAt         = DateTime.UtcNow,
            CreatedBy         = "Agent"
        };

        await db.Companies.AddAsync(company, ct);
        await db.SaveChangesAsync(ct);

        return Ok(new { companyId = company.Id, companyCode = code, success = true,
                        message = $"Company '{company.CompanyName}' created successfully with code {code}." });
    }

    // ── update_company ───────────────────────────────────────────────────────

    private async Task<string> UpdateCompanyAsync(JsonObject args, CancellationToken ct)
    {
        if (!Guid.TryParse(Str(args, "companyId"), out var id))
            return Fail("companyId must be a valid GUID.");

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (company is null) return Fail($"Company {id} not found.");

        ApplyIfPresent(args, "companyName",       v => company.CompanyName       = v);
        ApplyIfPresent(args, "primaryAddress",    v => company.PrimaryAddress    = v);
        ApplyIfPresent(args, "primaryCity",       v => company.PrimaryCity       = v);
        ApplyIfPresent(args, "primaryState",      v => company.PrimaryState      = v);
        ApplyIfPresent(args, "primaryPostalCode", v => company.PrimaryPostalCode = v);
        ApplyNullable(args,  "secondaryEmail",    v => company.SecondaryEmail    = v);
        ApplyNullable(args,  "phoneNumber",       v => company.PhoneNumber       = v);
        ApplyNullable(args,  "secondaryAddress",  v => company.SecondaryAddress  = v);
        ApplyNullable(args,  "secondaryCity",     v => company.SecondaryCity     = v);
        ApplyNullable(args,  "secondaryState",    v => company.SecondaryState    = v);
        ApplyNullable(args,  "secondaryPostalCode", v => company.SecondaryPostalCode = v);
        ApplyNullable(args,  "websiteUrl",        v => company.WebsiteUrl        = v);

        if (args.ContainsKey("primaryEmail"))
        {
            var newEmail = Trim(Str(args, "primaryEmail"));
            var emailTaken = await db.Companies
                .AnyAsync(c => c.PrimaryEmail.ToLower() == newEmail.ToLower() && c.Id != id, ct);
            if (emailTaken) return Fail($"Email '{newEmail}' is already used by another company.");
            company.PrimaryEmail = newEmail;
        }

        company.UpdatedAt = DateTime.UtcNow;
        company.UpdatedBy = "Agent";
        await db.SaveChangesAsync(ct);

        return Ok(new { success = true, message = "Company updated successfully.", company = ToDto(company) });
    }

    // ── list_companies ───────────────────────────────────────────────────────

    private async Task<string> ListCompaniesAsync(JsonObject args, CancellationToken ct)
    {
        var query = db.Companies.AsNoTracking().AsQueryable();

        var search = NullTrim(args, "search");
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c =>
                c.CompanyCode.ToLower().Contains(s) ||
                c.CompanyName.ToLower().Contains(s) ||
                c.PrimaryEmail.ToLower().Contains(s) ||
                c.PrimaryCity.ToLower().Contains(s) ||
                c.PrimaryState.ToLower().Contains(s));
        }

        var city = NullTrim(args, "city");
        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(c => c.PrimaryCity.ToLower().Contains(city.ToLower()));

        var state = NullTrim(args, "state");
        if (!string.IsNullOrWhiteSpace(state))
            query = query.Where(c => c.PrimaryState.ToLower().Contains(state.ToLower()));

        if (args["isActive"]?.GetValue<bool>() is bool active)
            query = query.Where(c => c.IsActive == active);

        var pageSize = Math.Clamp(args["pageSize"]?.GetValue<int>() ?? 20, 1, 100);

        var items = await query
            .OrderBy(c => c.CompanyCode)
            .Take(pageSize)
            .Select(c => new { c.Id, c.CompanyCode, c.CompanyName, c.PrimaryEmail, c.PrimaryCity, c.PrimaryState, c.IsActive, c.CreatedAt })
            .ToListAsync(ct);

        return Ok(new { success = true, total = items.Count, companies = items });
    }

    // ── get_company ──────────────────────────────────────────────────────────

    private async Task<string> GetCompanyAsync(JsonObject args, CancellationToken ct)
    {
        Company? company = null;

        var idStr = NullTrim(args, "companyId");
        if (!string.IsNullOrWhiteSpace(idStr) && Guid.TryParse(idStr, out var id))
            company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

        if (company is null)
        {
            var name = NullTrim(args, "companyName");
            if (!string.IsNullOrWhiteSpace(name))
                company = await db.Companies.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CompanyName.ToLower().Contains(name.ToLower()), ct);
        }

        if (company is null) return Fail("Company not found.");
        return Ok(new { success = true, company = ToDto(company) });
    }

    // ── delete_company ───────────────────────────────────────────────────────

    private async Task<string> DeleteCompanyAsync(JsonObject args, CancellationToken ct)
    {
        if (!Guid.TryParse(Str(args, "companyId"), out var id))
            return Fail("companyId must be a valid GUID.");

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (company is null) return Fail($"Company {id} not found.");

        company.IsDeleted = true;
        company.IsActive  = false;
        company.UpdatedAt = DateTime.UtcNow;
        company.UpdatedBy = "Agent";
        await db.SaveChangesAsync(ct);

        return Ok(new { success = true, message = $"Company '{company.CompanyName}' deleted successfully." });
    }

    // ── validate_company_data ────────────────────────────────────────────────

    private string ValidateCompanyData(JsonObject args)
    {
        var operation = Str(args, "operation");
        var fields    = args["fields"] as JsonObject ?? new JsonObject();

        var missing = new List<string>();
        var errors  = new List<string>();

        if (operation == "create")
        {
            CheckRequired(fields, "companyName",       "Company Name",       missing);
            CheckRequired(fields, "primaryEmail",      "Primary Email",      missing);
            CheckRequired(fields, "primaryAddress",    "Primary Address",    missing);
            CheckRequired(fields, "primaryCity",       "Primary City",       missing);
            CheckRequired(fields, "primaryState",      "Primary State",      missing);
            CheckRequired(fields, "primaryPostalCode", "Primary Postal Code", missing);
        }

        ValidateEmailField(fields, "primaryEmail",   errors);
        ValidateEmailField(fields, "secondaryEmail", errors);
        ValidatePhone(fields, errors);
        ValidateUrl(fields, errors);
        ValidateLengths(fields, errors);

        return Ok(new
        {
            isValid      = missing.Count == 0 && errors.Count == 0,
            missingFields = missing,
            formatErrors  = errors
        });
    }

    // ── preview_company_data ─────────────────────────────────────────────────

    private async Task<string> PreviewCompanyDataAsync(JsonObject args, CancellationToken ct)
    {
        var operation = Str(args, "operation");
        var fields    = args["fields"] as JsonObject ?? new JsonObject();

        object? existing = null;
        if (operation == "update")
        {
            var idStr = NullTrim(args, "companyId");
            if (!string.IsNullOrWhiteSpace(idStr) && Guid.TryParse(idStr, out var id))
            {
                var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
                if (company is not null) existing = ToDto(company);
            }
        }

        return Ok(new
        {
            operation,
            existing,
            proposed = fields,
            preview  = new
            {
                companyName       = NullStr(fields, "companyName"),
                primaryEmail      = NullStr(fields, "primaryEmail"),
                secondaryEmail    = NullStr(fields, "secondaryEmail"),
                phoneNumber       = NullStr(fields, "phoneNumber"),
                primaryAddress    = NullStr(fields, "primaryAddress"),
                primaryCity       = NullStr(fields, "primaryCity"),
                primaryState      = NullStr(fields, "primaryState"),
                primaryPostalCode = NullStr(fields, "primaryPostalCode"),
                secondaryAddress  = NullStr(fields, "secondaryAddress"),
                secondaryCity     = NullStr(fields, "secondaryCity"),
                secondaryState    = NullStr(fields, "secondaryState"),
                secondaryPostalCode = NullStr(fields, "secondaryPostalCode"),
                websiteUrl        = NullStr(fields, "websiteUrl")
            }
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string> GenerateCompanyCodeAsync(CancellationToken ct)
    {
        var lastCode = await db.Companies
            .IgnoreQueryFilters()
            .OrderByDescending(c => c.CompanyCode)
            .Select(c => c.CompanyCode)
            .FirstOrDefaultAsync(ct);

        int next = 1;
        if (lastCode is not null && lastCode.StartsWith("COMP-") &&
            int.TryParse(lastCode[5..], out var parsed))
            next = parsed + 1;

        return $"COMP-{next:D4}";
    }

    private static object ToDto(Company c) => new
    {
        c.Id, c.CompanyCode, c.CompanyName, c.PrimaryEmail, c.SecondaryEmail, c.PhoneNumber,
        c.PrimaryAddress, c.PrimaryCity, c.PrimaryState, c.PrimaryPostalCode,
        c.SecondaryAddress, c.SecondaryCity, c.SecondaryState, c.SecondaryPostalCode,
        c.WebsiteUrl, c.IsActive, c.CreatedAt, c.UpdatedAt
    };

    private static string Ok(object data)    => JsonSerializer.Serialize(data, JsonOpts);
    private static string Fail(string msg)   => JsonSerializer.Serialize(new { success = false, error = msg }, JsonOpts);

    private static string  Str(JsonObject obj, string key)     => obj[key]?.GetValue<string>() ?? string.Empty;
    private static string? NullStr(JsonObject obj, string key) => obj[key]?.GetValue<string>();
    private static string  Trim(string v)                      => v.Trim();
    private static string? NullTrim(JsonObject obj, string key)
    {
        var v = NullStr(obj, key);
        return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    }

    private static void ApplyIfPresent(JsonObject obj, string key, Action<string> setter)
    {
        if (obj.ContainsKey(key) && !string.IsNullOrWhiteSpace(obj[key]?.GetValue<string>()))
            setter(obj[key]!.GetValue<string>().Trim());
    }

    private static void ApplyNullable(JsonObject obj, string key, Action<string?> setter)
    {
        if (obj.ContainsKey(key))
            setter(NullTrim(obj, key));
    }

    private static void CheckRequired(JsonObject fields, string key, string label, List<string> missing)
    {
        if (string.IsNullOrWhiteSpace(fields[key]?.GetValue<string>()))
            missing.Add(label);
    }

    private static void ValidateEmailField(JsonObject fields, string key, List<string> errors)
    {
        var v = NullTrim(fields, key);
        if (v is null) return;
        if (!Regex.IsMatch(v, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            errors.Add($"'{key}' is not a valid email address.");
    }

    private static void ValidatePhone(JsonObject fields, List<string> errors)
    {
        var v = NullTrim(fields, "phoneNumber");
        if (v is null) return;
        if (!Regex.IsMatch(v, @"^\+?[\d\s\-\(\)]{7,20}$"))
            errors.Add("'phoneNumber' is not a valid phone number format.");
    }

    private static void ValidateUrl(JsonObject fields, List<string> errors)
    {
        var v = NullTrim(fields, "websiteUrl");
        if (v is null) return;
        if (!Uri.TryCreate(v, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
            errors.Add("'websiteUrl' must be a valid http/https URL.");
    }

    private static readonly Dictionary<string, int> MaxLengths = new()
    {
        ["companyName"]         = 200,
        ["primaryEmail"]        = 200,
        ["secondaryEmail"]      = 200,
        ["phoneNumber"]         = 20,
        ["primaryAddress"]      = 300,
        ["primaryCity"]         = 100,
        ["primaryState"]        = 50,
        ["primaryPostalCode"]   = 10,
        ["secondaryAddress"]    = 300,
        ["secondaryCity"]       = 100,
        ["secondaryState"]      = 50,
        ["secondaryPostalCode"] = 10,
        ["websiteUrl"]          = 500
    };

    private static void ValidateLengths(JsonObject fields, List<string> errors)
    {
        foreach (var (key, max) in MaxLengths)
        {
            var v = NullStr(fields, key);
            if (v is not null && v.Length > max)
                errors.Add($"'{key}' exceeds maximum length of {max} characters.");
        }
    }
}
