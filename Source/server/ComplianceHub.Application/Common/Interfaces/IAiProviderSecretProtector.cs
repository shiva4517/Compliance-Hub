namespace ComplianceHub.Application.Common.Interfaces;

public interface IAiProviderSecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}
