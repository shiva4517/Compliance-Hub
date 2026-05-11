using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities;

public class AgentConversation : BaseEntity
{
    public Guid SecurityUserId { get; set; }
    public SecurityUser? SecurityUser { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "in_progress";
    public string? LastOperation { get; set; }
    public string? ContextDataJson { get; set; }

    public ICollection<AgentMessage> Messages { get; set; } = [];
}
