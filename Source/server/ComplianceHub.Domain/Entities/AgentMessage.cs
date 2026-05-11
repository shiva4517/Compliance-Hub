using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities;

public class AgentMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public AgentConversation? Conversation { get; set; }

    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ToolName { get; set; }
    public string? ToolPayloadJson { get; set; }
}
