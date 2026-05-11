using System.Text.Json;
using System.Text.Json.Nodes;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Agent.Models;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using ComplianceHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Infrastructure.Mcp;

public class SubscriptionMcpService(
    ComplianceHubDbContext db,
    RegulationsDbContext regDb,
    ICurrentUserService currentUser) : ISubscriptionMcpService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // ── Tool definitions ──────────────────────────────────────────────────────

    public IReadOnlyList<LlmToolDefinition> GetToolDefinitions() =>
    [
        new("find_customer",
            "Search for customers by name or customer code (partial/fuzzy match). Returns matching customers scoped to the current admin's company. Call this whenever the user mentions a customer name or code.",
            new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string", description = "Customer name, partial name, or customer code to search for" }
                },
                required = new[] { "name" }
            }),

        new("search_all_levels",
            "Search for any regulation node by name or section number across ALL 6 hierarchy levels simultaneously: GovernmentEntity, Agency, RegulationCategory, RegulationType, RegulationSubtype, Regulation. " +
            "ALWAYS use this tool when the user provides any regulation name, section number, or regulation-related text. " +
            "Returns matches grouped by level. Each match includes all parent IDs (governmentEntityId, agencyId, etc.) needed directly for preview_subscription and create_subscription.",
            new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string", description = "Regulation name, section number, identifier, or any partial text to search across all hierarchy levels" }
                },
                required = new[] { "name" }
            }),

        new("preview_subscription",
            "Build a combined preview showing multiple customers and the regulation hierarchy path before creating subscriptions. " +
            "Checks each customer for duplicate subscriptions and reports which ones already have this subscription. " +
            "Call this after both customers and regulation node are resolved, and before calling create_subscription.",
            new
            {
                type = "object",
                properties = new
                {
                    customerIds = new
                    {
                        type = "array",
                        items = new { type = "string" },
                        description = "Array of customer UUIDs to preview subscriptions for"
                    },
                    governmentEntityId   = new { type = "string", description = "UUID of the GovernmentEntity (always required)" },
                    agencyId             = new { type = "string", description = "UUID of the Agency — include only if subscribing at Agency level or deeper" },
                    regulationCategoryId = new { type = "string", description = "UUID of the RegulationCategory — include only if subscribing at Category level or deeper" },
                    regulationTypeId     = new { type = "string", description = "UUID of the RegulationType — include only if subscribing at Type level or deeper" },
                    regulationSubtypeId  = new { type = "string", description = "UUID of the RegulationSubtype — include only if subscribing at SubType level or deeper" },
                    regulationId         = new { type = "string", description = "UUID of the Regulation — include only if subscribing at Regulation level" }
                },
                required = new[] { "customerIds", "governmentEntityId" }
            }),

        new("create_subscription",
            "Create subscriptions for one or more customers to a regulation node after the user has confirmed the preview. " +
            "Pass only the customerIds that should be created (exclude any the user chose to skip due to duplicates). " +
            "Loops internally through all customerIds and creates one subscription per customer.",
            new
            {
                type = "object",
                properties = new
                {
                    customerIds = new
                    {
                        type = "array",
                        items = new { type = "string" },
                        description = "Array of customer UUIDs to create subscriptions for (only non-duplicate customers)"
                    },
                    governmentEntityId   = new { type = "string", description = "UUID of the GovernmentEntity (always required)" },
                    agencyId             = new { type = "string", description = "UUID of the Agency — include only if subscribing at Agency level or deeper" },
                    regulationCategoryId = new { type = "string", description = "UUID of the RegulationCategory — include only if subscribing at Category level or deeper" },
                    regulationTypeId     = new { type = "string", description = "UUID of the RegulationType — include only if subscribing at Type level or deeper" },
                    regulationSubtypeId  = new { type = "string", description = "UUID of the RegulationSubtype — include only if subscribing at SubType level or deeper" },
                    regulationId         = new { type = "string", description = "UUID of the Regulation — include only if subscribing at Regulation level" }
                },
                required = new[] { "customerIds", "governmentEntityId" }
            }),

        new("delete_subscription",
            "Soft-delete a subscription by its ID. Show the subscription details first and require explicit user confirmation before calling.",
            new
            {
                type = "object",
                properties = new
                {
                    subscriptionId = new { type = "string", description = "UUID of the subscription to delete" }
                },
                required = new[] { "subscriptionId" }
            }),

        new("list_subscriptions",
            "List all active subscriptions for a customer. Returns subscription IDs, hierarchy node names, and subscribing levels.",
            new
            {
                type = "object",
                properties = new
                {
                    customerId = new { type = "string", description = "UUID of the customer" }
                },
                required = new[] { "customerId" }
            }),

        new("get_subscription",
            "Get full details of a single subscription including the full hierarchy path.",
            new
            {
                type = "object",
                properties = new
                {
                    subscriptionId = new { type = "string", description = "UUID of the subscription" }
                },
                required = new[] { "subscriptionId" }
            })
    ];

    // ── Dispatch ──────────────────────────────────────────────────────────────

    public Task<string> InvokeToolAsync(string toolName, string argumentsJson, CancellationToken ct)
    {
        var args = JsonNode.Parse(argumentsJson) as JsonObject ?? [];
        return toolName switch
        {
            "find_customer"        => FindCustomerAsync(args, ct),
            "search_all_levels"    => SearchAllLevelsAsync(args, ct),
            "preview_subscription" => PreviewSubscriptionAsync(args, ct),
            "create_subscription"  => CreateSubscriptionAsync(args, ct),
            "delete_subscription"  => DeleteSubscriptionAsync(args, ct),
            "list_subscriptions"   => ListSubscriptionsAsync(args, ct),
            "get_subscription"     => GetSubscriptionAsync(args, ct),
            _                      => Task.FromResult(Fail($"Unknown tool: {toolName}"))
        };
    }

    // ── find_customer ─────────────────────────────────────────────────────────

    private async Task<string> FindCustomerAsync(JsonObject args, CancellationToken ct)
    {
        var name = Str(args, "name");
        if (string.IsNullOrWhiteSpace(name)) return Fail("name is required");

        var companyId = currentUser.UserId;
        if (companyId is null) return Fail("Could not determine company.");

        var lower = name.ToLower();
        var customers = await db.Set<Customer>()
            .Where(c => c.CompanyId == companyId && !c.IsDeleted &&
                        (c.CustomerName.ToLower().Contains(lower) ||
                         c.CustomerCode.ToLower().Contains(lower)))
            .OrderBy(c => c.CustomerName)
            .Take(10)
            .Select(c => new { id = c.Id, customerCode = c.CustomerCode, customerName = c.CustomerName, primaryEmail = c.PrimaryEmail })
            .ToListAsync(ct);

        if (customers.Count == 0)
            return Fail($"No customers found matching \"{name}\". Please check the name and try again.");

        if (customers.Count == 1)
            return Ok(new { count = 1, exactMatch = true, customers, message = $"Found 1 customer: {customers[0].customerName}" });

        return Ok(new { count = customers.Count, exactMatch = false, customers, message = $"Found {customers.Count} customers matching \"{name}\". Please tell me which one(s) to use." });
    }

    // ── search_all_levels ─────────────────────────────────────────────────────

    private async Task<string> SearchAllLevelsAsync(JsonObject args, CancellationToken ct)
    {
        var name = Str(args, "name");
        if (string.IsNullOrWhiteSpace(name)) return Fail("name is required");

        var lower = name.ToLower();
        var results = new List<object>();

        var entities = await regDb.Set<ComplianceHub.Domain.Entities.Regulations.GovernmentEntity>()
            .Where(e => !e.IsDeleted && (e.TitleName.ToLower().Contains(lower) || e.Identifier.ToLower().Contains(lower)))
            .OrderBy(e => e.TitleName).Take(5)
            .Select(e => new
            {
                id = e.Id, identifier = e.Identifier, name = e.TitleName, titleNumber = e.TitleNumber,
                governmentEntityId = e.Id,
                agencyId             = (Guid?)null,
                regulationCategoryId = (Guid?)null,
                regulationTypeId     = (Guid?)null,
                regulationSubtypeId  = (Guid?)null,
                regulationId         = (Guid?)null
            })
            .ToListAsync(ct);
        if (entities.Count > 0)
            results.Add(new { level = "Entity", subscribingLevel = "Entity", count = entities.Count, matches = (object)entities });

        var agencies = await regDb.Set<ComplianceHub.Domain.Entities.Regulations.Agency>()
            .Where(a => !a.IsDeleted && (a.AgencyName.ToLower().Contains(lower) || a.Identifier.ToLower().Contains(lower)))
            .OrderBy(a => a.AgencyName).Take(5)
            .Select(a => new
            {
                id = a.Id, identifier = a.Identifier, name = a.AgencyName,
                governmentEntityId   = a.GovernmentEntityId,
                agencyId             = (Guid?)a.Id,
                regulationCategoryId = (Guid?)null,
                regulationTypeId     = (Guid?)null,
                regulationSubtypeId  = (Guid?)null,
                regulationId         = (Guid?)null
            })
            .ToListAsync(ct);
        if (agencies.Count > 0)
            results.Add(new { level = "Agency", subscribingLevel = "Agency", count = agencies.Count, matches = (object)agencies });

        var categories = await regDb.Set<ComplianceHub.Domain.Entities.Regulations.RegulationCategory>()
            .Where(c => !c.IsDeleted && (c.SubchapterName.ToLower().Contains(lower) || c.Identifier.ToLower().Contains(lower)))
            .OrderBy(c => c.SubchapterName).Take(5)
            .Select(c => new
            {
                id = c.Id, identifier = c.Identifier, name = c.SubchapterName,
                governmentEntityId   = c.GovernmentEntityId,
                agencyId             = (Guid?)c.AgencyId,
                regulationCategoryId = (Guid?)c.Id,
                regulationTypeId     = (Guid?)null,
                regulationSubtypeId  = (Guid?)null,
                regulationId         = (Guid?)null
            })
            .ToListAsync(ct);
        if (categories.Count > 0)
            results.Add(new { level = "Category", subscribingLevel = "Category", count = categories.Count, matches = (object)categories });

        var types = await regDb.Set<ComplianceHub.Domain.Entities.Regulations.RegulationType>()
            .Where(t => !t.IsDeleted && (t.PartName.ToLower().Contains(lower) || t.Identifier.ToLower().Contains(lower)))
            .OrderBy(t => t.PartName).Take(5)
            .Select(t => new
            {
                id = t.Id, identifier = t.Identifier, name = t.PartName,
                governmentEntityId   = t.GovernmentEntityId,
                agencyId             = (Guid?)t.AgencyId,
                regulationCategoryId = (Guid?)t.RegulationCategoryId,
                regulationTypeId     = (Guid?)t.Id,
                regulationSubtypeId  = (Guid?)null,
                regulationId         = (Guid?)null
            })
            .ToListAsync(ct);
        if (types.Count > 0)
            results.Add(new { level = "Type", subscribingLevel = "Type", count = types.Count, matches = (object)types });

        var subtypes = await regDb.Set<ComplianceHub.Domain.Entities.Regulations.RegulationSubtype>()
            .Where(s => !s.IsDeleted && (s.SubpartName.ToLower().Contains(lower) || s.Identifier.ToLower().Contains(lower)))
            .OrderBy(s => s.SubpartName).Take(5)
            .Select(s => new
            {
                id = s.Id, identifier = s.Identifier, name = s.SubpartName,
                governmentEntityId   = s.RegulationType!.GovernmentEntityId,
                agencyId             = (Guid?)s.RegulationType!.AgencyId,
                regulationCategoryId = (Guid?)s.RegulationType!.RegulationCategoryId,
                regulationTypeId     = (Guid?)s.RegulationTypeId,
                regulationSubtypeId  = (Guid?)s.Id,
                regulationId         = (Guid?)null
            })
            .ToListAsync(ct);
        if (subtypes.Count > 0)
            results.Add(new { level = "SubType", subscribingLevel = "SubType", count = subtypes.Count, matches = (object)subtypes });

        var regulations = await regDb.Set<ComplianceHub.Domain.Entities.Regulations.Regulation>()
            .Where(r => !r.IsDeleted && r.IsActive &&
                        (r.SectionName.ToLower().Contains(lower) ||
                         r.Identifier.ToLower().Contains(lower) ||
                         r.SectionNumber.ToLower().Contains(lower)))
            .OrderBy(r => r.SectionName).Take(5)
            .Select(r => new
            {
                id = r.Id, identifier = r.Identifier, sectionNumber = r.SectionNumber, name = r.SectionName,
                governmentEntityId   = r.GovernmentEntityId,
                agencyId             = (Guid?)r.AgencyId,
                regulationCategoryId = (Guid?)r.RegulationCategoryId,
                regulationTypeId     = (Guid?)r.RegulationTypeId,
                regulationSubtypeId  = r.RegulationSubtypeId,
                regulationId         = (Guid?)r.Id
            })
            .ToListAsync(ct);
        if (regulations.Count > 0)
            results.Add(new { level = "Regulation", subscribingLevel = "Regulation", count = regulations.Count, matches = (object)regulations });

        if (results.Count == 0)
            return Fail($"No regulation nodes found matching \"{name}\" in any hierarchy level (GovernmentEntity, Agency, Category, Type, SubType, Regulation). Try a different search term.");

        var total = entities.Count + agencies.Count + categories.Count + types.Count + subtypes.Count + regulations.Count;
        return Ok(new
        {
            searchTerm   = name,
            totalMatches = total,
            instruction  = "Each match includes governmentEntityId, agencyId, regulationCategoryId, regulationTypeId, regulationSubtypeId, regulationId — pass these directly to preview_subscription or create_subscription. Only fields relevant to the match level will be non-null.",
            levels       = results
        });
    }

    // ── preview_subscription ──────────────────────────────────────────────────

    private async Task<string> PreviewSubscriptionAsync(JsonObject args, CancellationToken ct)
    {
        var entityId = ParseGuid(args, "governmentEntityId");
        if (entityId is null) return Fail("governmentEntityId is required");

        var customerIds = ParseGuidArray(args, "customerIds");
        if (customerIds.Count == 0) return Fail("customerIds array is required and must not be empty");

        var agencyId             = ParseGuid(args, "agencyId");
        var regulationCategoryId = ParseGuid(args, "regulationCategoryId");
        var regulationTypeId     = ParseGuid(args, "regulationTypeId");
        var regulationSubtypeId  = ParseGuid(args, "regulationSubtypeId");
        var regulationId         = ParseGuid(args, "regulationId");

        var hierarchy = await BuildHierarchyPathAsync(entityId.Value, agencyId, regulationCategoryId, regulationTypeId, regulationSubtypeId, regulationId, ct);
        if (hierarchy.Error is not null) return Fail(hierarchy.Error);

        var companyId = currentUser.UserId;
        var customerResults = new List<object>();
        var duplicateCustomers = new List<object>();
        var newCustomers       = new List<object>();

        foreach (var cid in customerIds)
        {
            var customer = await db.Set<Customer>()
                .Where(c => c.Id == cid && c.CompanyId == companyId && !c.IsDeleted)
                .Select(c => new { c.Id, c.CustomerName, c.CustomerCode })
                .FirstOrDefaultAsync(ct);

            if (customer is null)
            {
                customerResults.Add(new { customerId = cid, customerName = "NOT FOUND", isDuplicate = false, error = "Customer not found in your company" });
                continue;
            }

            var isDuplicate = await db.Set<Subscription>().AnyAsync(s =>
                !s.IsDeleted && s.CustomerId == cid
                && s.GovernmentEntityId   == entityId
                && s.AgencyId             == agencyId
                && s.RegulationCategoryId == regulationCategoryId
                && s.RegulationTypeId     == regulationTypeId
                && s.RegulationSubtypeId  == regulationSubtypeId
                && s.RegulationId         == regulationId, ct);

            var entry = new { customerId = customer.Id, customerName = customer.CustomerName, customerCode = customer.CustomerCode, isDuplicate };
            customerResults.Add(entry);
            if (isDuplicate) duplicateCustomers.Add(entry);
            else             newCustomers.Add(entry);
        }

        return Ok(new
        {
            customers            = customerResults,
            newCustomers,
            duplicateCustomers,
            hierarchyPath        = hierarchy.Path,
            subscribingLevel     = hierarchy.Level.ToString(),
            subscribedNodeName   = hierarchy.NodeName,
            governmentEntityId   = entityId,
            agencyId,
            regulationCategoryId,
            regulationTypeId,
            regulationSubtypeId,
            regulationId,
            summary = duplicateCustomers.Count > 0
                ? $"{newCustomers.Count} customer(s) will be subscribed. {duplicateCustomers.Count} customer(s) already have this subscription and will be skipped if confirmed."
                : $"{newCustomers.Count} customer(s) will be subscribed."
        });
    }

    // ── create_subscription ───────────────────────────────────────────────────

    private async Task<string> CreateSubscriptionAsync(JsonObject args, CancellationToken ct)
    {
        var entityId = ParseGuid(args, "governmentEntityId");
        if (entityId is null) return Fail("governmentEntityId is required");

        var customerIds = ParseGuidArray(args, "customerIds");
        if (customerIds.Count == 0) return Fail("customerIds array is required and must not be empty");

        var agencyId             = ParseGuid(args, "agencyId");
        var regulationCategoryId = ParseGuid(args, "regulationCategoryId");
        var regulationTypeId     = ParseGuid(args, "regulationTypeId");
        var regulationSubtypeId  = ParseGuid(args, "regulationSubtypeId");
        var regulationId         = ParseGuid(args, "regulationId");

        var hierarchy = await BuildHierarchyPathAsync(entityId.Value, agencyId, regulationCategoryId, regulationTypeId, regulationSubtypeId, regulationId, ct);
        if (hierarchy.Error is not null) return Fail(hierarchy.Error);

        var companyId = currentUser.UserId;
        var created = new List<object>();
        var skipped = new List<object>();
        var failed  = new List<object>();

        foreach (var cid in customerIds)
        {
            var customer = await db.Set<Customer>()
                .Where(c => c.Id == cid && c.CompanyId == companyId && !c.IsDeleted)
                .FirstOrDefaultAsync(ct);

            if (customer is null)
            {
                failed.Add(new { customerId = cid, reason = "Customer not found in your company" });
                continue;
            }

            var alreadyExists = await db.Set<Subscription>().AnyAsync(s =>
                !s.IsDeleted && s.CustomerId == cid
                && s.GovernmentEntityId   == entityId
                && s.AgencyId             == agencyId
                && s.RegulationCategoryId == regulationCategoryId
                && s.RegulationTypeId     == regulationTypeId
                && s.RegulationSubtypeId  == regulationSubtypeId
                && s.RegulationId         == regulationId, ct);

            if (alreadyExists)
            {
                skipped.Add(new { customerId = cid, customerName = customer.CustomerName, reason = "Subscription already exists" });
                continue;
            }

            var sub = new Subscription
            {
                CustomerId           = cid,
                GovernmentEntityId   = entityId.Value,
                AgencyId             = agencyId,
                RegulationCategoryId = regulationCategoryId,
                RegulationTypeId     = regulationTypeId,
                RegulationSubtypeId  = regulationSubtypeId,
                RegulationId         = regulationId,
                SubscribingLevel     = hierarchy.Level,
                SubscribedNodeName   = hierarchy.NodeName,
                IsActive             = true,
                CreatedAt            = DateTime.UtcNow,
                CreatedBy            = currentUser.Email
            };
            db.Set<Subscription>().Add(sub);
            created.Add(new { subscriptionId = sub.Id, customerId = cid, customerName = customer.CustomerName });
        }

        if (created.Count > 0)
            await db.SaveChangesAsync(ct);

        return Ok(new
        {
            createdCount   = created.Count,
            skippedCount   = skipped.Count,
            failedCount    = failed.Count,
            created,
            skipped,
            failed,
            hierarchyPath      = hierarchy.Path,
            subscribingLevel   = hierarchy.Level.ToString(),
            subscribedNodeName = hierarchy.NodeName,
            summary = $"Successfully created {created.Count} subscription(s)." +
                      (skipped.Count > 0 ? $" {skipped.Count} skipped (already existed)." : "") +
                      (failed.Count  > 0 ? $" {failed.Count} failed." : "")
        });
    }

    // ── delete_subscription ───────────────────────────────────────────────────

    private async Task<string> DeleteSubscriptionAsync(JsonObject args, CancellationToken ct)
    {
        if (!Guid.TryParse(Str(args, "subscriptionId"), out var subId))
            return Fail("subscriptionId is required and must be a valid UUID");

        var companyId = currentUser.UserId;
        var sub = await db.Set<Subscription>()
            .Include(s => s.Customer)
            .Where(s => s.Id == subId && !s.IsDeleted && s.Customer!.CompanyId == companyId)
            .FirstOrDefaultAsync(ct);

        if (sub is null) return Fail("Subscription not found.");

        sub.IsDeleted = true;
        sub.IsActive  = false;
        sub.UpdatedAt = DateTime.UtcNow;
        sub.UpdatedBy = currentUser.Email;
        await db.SaveChangesAsync(ct);

        return Ok(new { subscriptionId = subId, customerName = sub.Customer!.CustomerName, message = "Subscription deleted successfully." });
    }

    // ── list_subscriptions ────────────────────────────────────────────────────

    private async Task<string> ListSubscriptionsAsync(JsonObject args, CancellationToken ct)
    {
        var customerId = ParseGuid(args, "customerId");
        if (customerId is null) return Fail("customerId is required");

        var companyId = currentUser.UserId;
        var customer = await db.Set<Customer>()
            .Where(c => c.Id == customerId && c.CompanyId == companyId && !c.IsDeleted)
            .FirstOrDefaultAsync(ct);
        if (customer is null) return Fail("Customer not found.");

        var subs = await db.Set<Subscription>()
            .Where(s => s.CustomerId == customerId && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                subscriptionId     = s.Id,
                subscribingLevel   = s.SubscribingLevel.ToString(),
                subscribedNodeName = s.SubscribedNodeName,
                isActive           = s.IsActive,
                createdAt          = s.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { customerName = customer.CustomerName, count = subs.Count, subscriptions = subs });
    }

    // ── get_subscription ──────────────────────────────────────────────────────

    private async Task<string> GetSubscriptionAsync(JsonObject args, CancellationToken ct)
    {
        if (!Guid.TryParse(Str(args, "subscriptionId"), out var subId))
            return Fail("subscriptionId is required");

        var companyId = currentUser.UserId;
        var sub = await db.Set<Subscription>()
            .Include(s => s.Customer)
            .Where(s => s.Id == subId && !s.IsDeleted && s.Customer!.CompanyId == companyId)
            .FirstOrDefaultAsync(ct);

        if (sub is null) return Fail("Subscription not found.");

        var hierarchy = await BuildHierarchyPathAsync(
            sub.GovernmentEntityId, sub.AgencyId, sub.RegulationCategoryId,
            sub.RegulationTypeId, sub.RegulationSubtypeId, sub.RegulationId, ct);

        return Ok(new
        {
            subscriptionId     = sub.Id,
            customerName       = sub.Customer!.CustomerName,
            hierarchyPath      = hierarchy.Path,
            subscribingLevel   = sub.SubscribingLevel.ToString(),
            subscribedNodeName = sub.SubscribedNodeName,
            isActive           = sub.IsActive,
            createdAt          = sub.CreatedAt
        });
    }

    // ── Hierarchy path builder ────────────────────────────────────────────────

    private async Task<HierarchyResolution> BuildHierarchyPathAsync(
        Guid entityId, Guid? agencyId, Guid? categoryId,
        Guid? typeId, Guid? subtypeId, Guid? regulationId,
        CancellationToken ct)
    {
        var path = new List<string>();

        var entity = await regDb.Set<ComplianceHub.Domain.Entities.Regulations.GovernmentEntity>()
            .Where(e => e.Id == entityId && !e.IsDeleted)
            .Select(e => new { e.TitleName })
            .FirstOrDefaultAsync(ct);
        if (entity is null) return HierarchyResolution.Fail("Government entity not found.");
        path.Add($"Government Entity: {entity.TitleName}");
        var nodeName = entity.TitleName;
        var level    = SubscribingLevel.Entity;

        if (agencyId.HasValue)
        {
            var agency = await regDb.Set<ComplianceHub.Domain.Entities.Regulations.Agency>()
                .Where(a => a.Id == agencyId && !a.IsDeleted)
                .Select(a => new { a.AgencyName })
                .FirstOrDefaultAsync(ct);
            if (agency is null) return HierarchyResolution.Fail("Agency not found.");
            path.Add($"Agency: {agency.AgencyName}");
            nodeName = agency.AgencyName;
            level    = SubscribingLevel.Agency;
        }

        if (categoryId.HasValue)
        {
            var cat = await regDb.Set<ComplianceHub.Domain.Entities.Regulations.RegulationCategory>()
                .Where(c => c.Id == categoryId && !c.IsDeleted)
                .Select(c => new { c.SubchapterName })
                .FirstOrDefaultAsync(ct);
            if (cat is null) return HierarchyResolution.Fail("Regulation category not found.");
            path.Add($"Category: {cat.SubchapterName}");
            nodeName = cat.SubchapterName;
            level    = SubscribingLevel.Category;
        }

        if (typeId.HasValue)
        {
            var typ = await regDb.Set<ComplianceHub.Domain.Entities.Regulations.RegulationType>()
                .Where(t => t.Id == typeId && !t.IsDeleted)
                .Select(t => new { t.PartName })
                .FirstOrDefaultAsync(ct);
            if (typ is null) return HierarchyResolution.Fail("Regulation type not found.");
            path.Add($"Type: {typ.PartName}");
            nodeName = typ.PartName;
            level    = SubscribingLevel.Type;
        }

        if (subtypeId.HasValue)
        {
            var sub = await regDb.Set<ComplianceHub.Domain.Entities.Regulations.RegulationSubtype>()
                .Where(s => s.Id == subtypeId && !s.IsDeleted)
                .Select(s => new { s.SubpartName })
                .FirstOrDefaultAsync(ct);
            if (sub is null) return HierarchyResolution.Fail("Regulation subtype not found.");
            path.Add($"SubType: {sub.SubpartName}");
            nodeName = sub.SubpartName;
            level    = SubscribingLevel.SubType;
        }

        if (regulationId.HasValue)
        {
            var reg = await regDb.Set<ComplianceHub.Domain.Entities.Regulations.Regulation>()
                .Where(r => r.Id == regulationId && !r.IsDeleted)
                .Select(r => new { r.SectionName })
                .FirstOrDefaultAsync(ct);
            if (reg is null) return HierarchyResolution.Fail("Regulation not found.");
            path.Add($"Regulation: {reg.SectionName}");
            nodeName = reg.SectionName;
            level    = SubscribingLevel.Regulation;
        }

        return new HierarchyResolution(string.Join(" → ", path), level, nodeName, null);
    }

    private record HierarchyResolution(string Path, SubscribingLevel Level, string NodeName, string? Error)
    {
        public static HierarchyResolution Fail(string msg) =>
            new(string.Empty, SubscribingLevel.Entity, string.Empty, msg);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Ok(object data) =>
        JsonSerializer.Serialize(new { success = true, data }, JsonOpts);

    private static string Fail(string message) =>
        JsonSerializer.Serialize(new { success = false, error = message }, JsonOpts);

    private static string Str(JsonObject obj, string key) =>
        obj[key]?.GetValue<string>() ?? string.Empty;

    private static Guid? ParseGuid(JsonObject obj, string key)
    {
        var s = Str(obj, key);
        return Guid.TryParse(s, out var g) ? g : null;
    }

    private static List<Guid> ParseGuidArray(JsonObject obj, string key)
    {
        var result = new List<Guid>();
        if (obj[key] is not JsonArray arr) return result;
        foreach (var item in arr)
        {
            if (item is JsonValue v && Guid.TryParse(v.GetValue<string>(), out var g))
                result.Add(g);
        }
        return result;
    }
}
