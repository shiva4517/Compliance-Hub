namespace ComplianceHub.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string fullName, string username, string temporaryPassword, CancellationToken ct = default);

    Task SendEmployeeWelcomeEmailAsync(
        string toEmail,
        string fullName,
        string companyName,
        string username,
        string temporaryPassword,
        CancellationToken ct = default);

    (string Subject, string Body) BuildRegulationChangeEmail(
        string recipientName,
        string customerName,
        string governmentEntityName,
        string previousRegulationName,
        string presentRegulationName,
        string changeType,
        string? regulationDetailsHtml);

    (string Subject, string Body) BuildSubscriptionConfirmationEmail(
        string recipientName,
        string customerName,
        IEnumerable<(string NodeName, string Level)> subscribedNodes);

    Task SendRawEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
    Task SendRawEmailOrThrowAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
