using ComplianceHub.Functions.Data.Models;

namespace ComplianceHub.Functions.Services;

public class EmailTemplateService
{
    public string Resolve(NotificationHistory n)
    {
        if (!string.IsNullOrWhiteSpace(n.Body))
            return n.Body;

        return GenerateHtml(n);
    }

    private static string GenerateHtml(NotificationHistory n)
    {
        var recipient = string.IsNullOrWhiteSpace(n.RecipientName) ? "Subscriber" : n.RecipientName;
        var sender = string.IsNullOrWhiteSpace(n.SenderName) ? "Compliance Hub" : n.SenderName;

        var rows = BuildTableRows(n);

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8" />
              <style>
                body { font-family: Arial, sans-serif; color: #333; margin: 0; padding: 0; }
                .wrapper { max-width: 680px; margin: 32px auto; background: #fff; border: 1px solid #e0e0e0; border-radius: 6px; overflow: hidden; }
                .header { background: #1e3a5f; color: #fff; padding: 24px 32px; }
                .header h1 { margin: 0; font-size: 20px; }
                .body { padding: 28px 32px; }
                .body p { line-height: 1.6; }
                table { width: 100%; border-collapse: collapse; margin-top: 16px; font-size: 14px; }
                th { text-align: left; background: #f4f6f8; padding: 8px 12px; color: #555; width: 36%; border-bottom: 1px solid #ddd; }
                td { padding: 8px 12px; border-bottom: 1px solid #eee; }
                .footer { background: #f9f9f9; padding: 16px 32px; font-size: 12px; color: #888; border-top: 1px solid #e0e0e0; }
              </style>
            </head>
            <body>
              <div class="wrapper">
                <div class="header">
                  <h1>Regulation Change Notice</h1>
                </div>
                <div class="body">
                  <p>Dear <strong>{{Encode(recipient)}}</strong>,</p>
                  <p>
                    A regulatory update has been detected that falls within your monitored subscription scope.
                    Please review the details below and take any necessary action.
                  </p>
                  <table>
                    {{rows}}
                  </table>
                  <p style="margin-top:24px;">
                    If you have questions, please contact your compliance team or reply to this email.
                  </p>
                  <p>Regards,<br/><strong>{{Encode(sender)}}</strong></p>
                </div>
                <div class="footer">
                  This is an automated notification from the Compliance Hub.
                  &copy; {{DateTime.UtcNow.Year}} Compliance Hub. All rights reserved.
                </div>
              </div>
            </body>
            </html>
            """;
    }

    private static string BuildTableRows(NotificationHistory n)
    {
        var rows = new List<(string Label, string? Value)>
        {
            ("Government Entity",   n.GovernmentEntityName),
            ("Agency",              n.AgencyName),
            ("Category",            n.RegulationCategoryName),
            ("Type",                n.RegulationTypeName),
            ("Subtype",             n.RegulationSubtypeName),
            ("Previous Regulation", n.PreviousRegulationName),
            ("Current Regulation",  n.PresentRegulationName),
            ("Change Detected At",  n.CreatedAt.ToString("MMM dd, yyyy HH:mm UTC")),
        };

        return string.Concat(rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Value))
            .Select(r => $"<tr><th>{Encode(r.Label)}</th><td>{Encode(r.Value!)}</td></tr>"));
    }

    private static string Encode(string s)
        => System.Net.WebUtility.HtmlEncode(s);
}
