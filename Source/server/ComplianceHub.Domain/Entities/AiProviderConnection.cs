using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities;

public class AiProviderConnection : BaseEntity
{
    public Guid SecurityUserId { get; set; }
    public SecurityUser? SecurityUser { get; set; }

    public string Provider { get; set; } = string.Empty;
    public string EncryptedApiKey { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string? DeploymentName { get; set; }
    public string? Model { get; set; }
    public string? ApiVersion { get; set; }
}
