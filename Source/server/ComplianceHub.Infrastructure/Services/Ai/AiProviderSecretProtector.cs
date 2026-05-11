using ComplianceHub.Application.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace ComplianceHub.Infrastructure.Services.Ai;

public class AiProviderSecretProtector(IDataProtectionProvider provider) : IAiProviderSecretProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("ComplianceHub.AiProviderConnection.ApiKey");

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
