using System.Text.Json.Serialization;

namespace ComplianceHub.Functions.Models;

public record EcfrTitle(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("latest_amended_on")] DateOnly? LatestAmendedOn);

public record EcfrNode(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("identifier")] string Identifier,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("label_description")] string? Heading,
    [property: JsonPropertyName("children")] List<EcfrNode> Children);

public record EcfrVersionEntry(
    [property: JsonPropertyName("identifier")] string Identifier,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("part")] string Part,
    [property: JsonPropertyName("subpart")] string SubPart,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("date")] DateOnly Date,
    [property: JsonPropertyName("amendment_date")] DateOnly AmendedDate,
    [property: JsonPropertyName("issue_date")] DateOnly issueDate,
    [property: JsonPropertyName("change_types")] List<string> ChangeTypes);

public record TitlesResponse([property: JsonPropertyName("titles")] List<EcfrTitle> Titles);
public record VersionsResponse([property: JsonPropertyName("content_versions")] List<EcfrVersionEntry> ContentVersions);
