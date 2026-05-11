using ComplianceHub.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ComplianceHub.Infrastructure.Services;

public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendWelcomeEmailAsync(string toEmail, string fullName, string username, string temporaryPassword, CancellationToken ct = default)
    {
        var loginUrl = config["Email:LoginUrl"] ?? "http://localhost:5173/login";
        var body = BuildCredentialEmailBody(fullName, username, temporaryPassword, loginUrl, null);
        await SendAsync(toEmail, "Your Compliance Hub Account — Action Required", body, ct);
    }

    public async Task SendEmployeeWelcomeEmailAsync(
        string toEmail,
        string fullName,
        string companyName,
        string username,
        string temporaryPassword,
        CancellationToken ct = default)
    {
        var loginUrl = config["Email:LoginUrl"] ?? "http://localhost:5173/login";
        var body = BuildCredentialEmailBody(fullName, username, temporaryPassword, loginUrl, companyName);
        await SendAsync(toEmail, $"Welcome to {companyName} — Your Compliance Hub Employee Account", body, ct);
    }

    private static string BuildCredentialEmailBody(string fullName, string username, string temporaryPassword, string loginUrl, string? companyName)
    {
        var companyLine = companyName is not null
            ? $"<p>You have been added as an employee of <strong>{companyName}</strong>.</p>"
            : string.Empty;

        return $@"
<html><body style=""font-family:Arial,sans-serif;max-width:600px;margin:0 auto;"">
  <div style=""background:#1e3a5f;padding:24px;border-radius:8px 8px 0 0;"">
    <h1 style=""color:white;margin:0;font-size:22px;"">Welcome to Compliance Hub</h1>
  </div>
  <div style=""background:#f9fafb;padding:24px;border-radius:0 0 8px 8px;border:1px solid #e5e7eb;"">
    <p>Hello <strong>{fullName}</strong>,</p>
    {companyLine}
    <p>Your Compliance Hub account has been created. Here are your login credentials:</p>
    <table style=""background:white;border:1px solid #e5e7eb;border-radius:6px;padding:16px;width:100%;margin:16px 0;"">
      <tr><td style=""color:#6b7280;padding:4px 0;"">Username / Email</td><td style=""font-weight:600;""><a href=""mailto:{username}"">{username}</a></td></tr>
      <tr><td style=""color:#6b7280;padding:4px 0;"">Temporary Password</td><td style=""font-family:monospace;font-size:16px;font-weight:600;color:#1e3a5f;letter-spacing:1px;"">{temporaryPassword}</td></tr>
    </table>
    <div style=""background:#fef3c7;border:1px solid #f59e0b;border-radius:6px;padding:12px;margin:16px 0;"">
      <strong>&#9888; Important:</strong> You must change your password on first login. Access to all modules will be restricted until your password is updated.
    </div>
    <p><a href=""{loginUrl}"" style=""background:#1e3a5f;color:white;padding:10px 20px;border-radius:6px;text-decoration:none;display:inline-block;"">Login to Compliance Hub</a></p>
    <p style=""color:#9ca3af;font-size:12px;margin-top:24px;"">If you did not expect this email, please contact your administrator.</p>
  </div>
</body></html>";
    }

    public (string Subject, string Body) BuildRegulationChangeEmail(
        string recipientName,
        string customerName,
        string governmentEntityName,
        string previousRegulationName,
        string presentRegulationName,
        string changeType,
        string? regulationDetailsHtml)
    {
        var subject = $"Regulation Update: {presentRegulationName} — {changeType}d";
        var detailsSection = string.IsNullOrWhiteSpace(regulationDetailsHtml)
            ? string.Empty
            : $@"
    <div style=""background:white;border:1px solid #e5e7eb;border-radius:6px;padding:16px;margin:16px 0;"">
      <h2 style=""margin:0 0 12px;font-size:16px;color:#1f2937;"">Regulation Details</h2>
      <div style=""color:#374151;line-height:1.6;"">{regulationDetailsHtml}</div>
    </div>";

        var body = $@"
<html><body style=""font-family:Arial,sans-serif;max-width:600px;margin:0 auto;"">
  <div style=""background:#1e3a5f;padding:24px;border-radius:8px 8px 0 0;"">
    <h1 style=""color:white;margin:0;font-size:20px;"">Regulation Change Notice</h1>
    <p style=""color:#93c5fd;margin:4px 0 0;"">Compliance Hub Notification</p>
  </div>
  <div style=""background:#f9fafb;padding:24px;border-radius:0 0 8px 8px;border:1px solid #e5e7eb;"">
    <p>Hello <strong>{recipientName}</strong>,</p>
    <p>A regulation that <strong>{customerName}</strong> is subscribed to has been <strong>{changeType.ToLower()}d</strong>.</p>
    <table style=""background:white;border:1px solid #e5e7eb;border-radius:6px;padding:16px;width:100%;margin:16px 0;border-collapse:collapse;"">
      <tr style=""border-bottom:1px solid #f3f4f6;"">
        <td style=""color:#6b7280;padding:8px 12px;width:40%;"">Government Entity</td>
        <td style=""font-weight:600;padding:8px 12px;"">{governmentEntityName}</td>
      </tr>
      <tr style=""border-bottom:1px solid #f3f4f6;"">
        <td style=""color:#6b7280;padding:8px 12px;"">Previous Regulation</td>
        <td style=""padding:8px 12px;"">{previousRegulationName}</td>
      </tr>
      <tr>
        <td style=""color:#6b7280;padding:8px 12px;"">Updated Regulation</td>
        <td style=""padding:8px 12px;font-weight:600;color:#1e3a5f;"">{presentRegulationName}</td>
      </tr>
    </table>
    {detailsSection}
    <p style=""color:#6b7280;font-size:13px;"">Please review the updated regulation in your Compliance Hub portal to ensure continued compliance.</p>
    <p style=""color:#9ca3af;font-size:12px;margin-top:24px;"">You are receiving this because your organization is subscribed to this regulation. Contact your administrator to manage subscriptions.</p>
  </div>
</body></html>";
        return (subject, body);
    }

    public (string Subject, string Body) BuildSubscriptionConfirmationEmail(
        string recipientName,
        string customerName,
        IEnumerable<(string NodeName, string Level)> subscribedNodes)
    {
        var subject = "Compliance Hub — Subscription Confirmation";

        var levelColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Entity"]     = "background:#7c3aed;color:white;",
            ["Agency"]     = "background:#2563eb;color:white;",
            ["Category"]   = "background:#0891b2;color:white;",
            ["Type"]       = "background:#16a34a;color:white;",
            ["SubType"]    = "background:#ca8a04;color:white;",
            ["Regulation"] = "background:#ea580c;color:white;",
        };

        var rows = string.Concat(subscribedNodes.Select(n =>
        {
            var style = levelColors.TryGetValue(n.Level, out var s) ? s : "background:#6b7280;color:white;";
            return $@"
      <tr style=""border-bottom:1px solid #f3f4f6;"">
        <td style=""padding:8px 12px;""><span style=""font-size:11px;font-weight:600;padding:2px 8px;border-radius:9999px;{style}"">{n.Level}</span></td>
        <td style=""padding:8px 12px;color:#1f2937;"">{n.NodeName}</td>
      </tr>";
        }));

        var body = $@"
<html><body style=""font-family:Arial,sans-serif;max-width:620px;margin:0 auto;"">
  <div style=""background:#1e3a5f;padding:24px;border-radius:8px 8px 0 0;"">
    <h1 style=""color:white;margin:0;font-size:20px;"">Subscription Confirmation</h1>
    <p style=""color:#93c5fd;margin:4px 0 0;"">Compliance Hub</p>
  </div>
  <div style=""background:#f9fafb;padding:24px;border-radius:0 0 8px 8px;border:1px solid #e5e7eb;"">
    <p>Hello <strong>{recipientName}</strong>,</p>
    <p>You have been successfully subscribed to the following regulation nodes on behalf of <strong>{customerName}</strong>. You will receive notifications whenever these regulations are updated.</p>
    <table style=""background:white;border:1px solid #e5e7eb;border-radius:6px;width:100%;margin:16px 0;border-collapse:collapse;"">
      <thead>
        <tr style=""background:#f3f4f6;"">
          <th style=""text-align:left;padding:8px 12px;font-size:12px;color:#6b7280;font-weight:600;width:120px;"">LEVEL</th>
          <th style=""text-align:left;padding:8px 12px;font-size:12px;color:#6b7280;font-weight:600;"">SUBSCRIBED NODE</th>
        </tr>
      </thead>
      <tbody>{rows}</tbody>
    </table>
    <p style=""color:#6b7280;font-size:13px;"">Log in to your Compliance Hub portal to view and manage your subscriptions.</p>
    <p style=""color:#9ca3af;font-size:12px;margin-top:24px;"">If you believe this was sent in error, please contact your administrator.</p>
  </div>
</body></html>";

        return (subject, body);
    }

    public async Task SendRawEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        => await SendAsync(toEmail, subject, htmlBody, ct, swallowExceptions: true);

    public async Task SendRawEmailOrThrowAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        => await SendAsync(toEmail, subject, htmlBody, ct, swallowExceptions: false);

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct, bool swallowExceptions = true)
    {
        var apiKey = config["Email:SendGridApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("SendGrid API key not configured. Email to {Email} skipped.", toEmail);
            if (!swallowExceptions)
                throw new InvalidOperationException("Email:SendGridApiKey is not configured.");
            return;
        }

        try
        {
            var fromAddress = config["Email:FromAddress"]
                ?? throw new InvalidOperationException("Email:FromAddress is not configured.");
            var fromName = config["Email:FromName"] ?? "Compliance Hub";

            var client  = new SendGridClient(apiKey);
            var from    = new EmailAddress(fromAddress, fromName);
            var to      = new EmailAddress(toEmail);
            var message = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: htmlBody);

            var response = await client.SendEmailAsync(message, ct);
            var statusCode = (int)response.StatusCode;

            if (statusCode >= 400)
            {
                var responseBody = await response.Body.ReadAsStringAsync(ct);
                logger.LogError(
                    "SendGrid rejected email to {Email} — HTTP {StatusCode}. From: {From}. Body: {ResponseBody}",
                    toEmail, statusCode, fromAddress, responseBody);
                throw new InvalidOperationException($"SendGrid returned {statusCode}: {responseBody}");
            }

            logger.LogInformation("Email sent to {Email} via SendGrid: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            if (!swallowExceptions)
                throw;
        }
    }
}
