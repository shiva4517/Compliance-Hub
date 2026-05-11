using System.Text.Json.Serialization;

namespace ComplianceHub.Application.Common.Models;

public class EcfrTitleDto
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("latest_amended_on")]
    public DateOnly? LatestAmendedOn { get; set; }

    [JsonPropertyName("latest_issue_date")]
    public DateOnly? LatestIssueDate { get; set; }
}

public class EcfrTitlesResponse
{
    [JsonPropertyName("titles")]
    public List<EcfrTitleDto> Titles { get; set; } = [];
}

public class EcfrStructureNodeDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("label_level")]
    public string? LabelLevel { get; set; }

    [JsonPropertyName("label_description")]
    public string? LabelDescription { get; set; }

    [JsonPropertyName("children")]
    public List<EcfrStructureNodeDto> Children { get; set; } = [];
}

public class EcfrStructureResponse
{
    [JsonPropertyName("structure")]
    public EcfrStructureNodeDto? Structure { get; set; }
}

public class EcfrVersionChangeDto
{
    [JsonPropertyName("date")]
    public string Date { get; set; }
    [JsonPropertyName("amendment_date")]
    public string AmendmentDate { get; set; }
    [JsonPropertyName("issue_date")]
    public string IssueDate { get; set; }
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("part")]
    public string Part { get; set; } = string.Empty;
    [JsonPropertyName("subpart")]
    public string SubPart { get; set; } = string.Empty;
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    //[JsonPropertyName("change_types")]
    //public List<string> ChangeTypes { get; set; } = [];
}

public class Meta
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("result_count")]
    public string ResultCount { get; set; } = string.Empty;
    [JsonPropertyName("latest_amendment_date")]
    public string LatestAmendmentDate { get; set; } = string.Empty;
}

public class EcfrVersionsResponse
{
    [JsonPropertyName("content_versions")]
    public List<EcfrVersionChangeDto> ContentVersions { get; set; } = [];
    [JsonPropertyName("meta")]
    public Meta Meta { get; set; }
}
