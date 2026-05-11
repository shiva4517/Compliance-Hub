using ComplianceHub.Application.Common.Models;

namespace ComplianceHub.Application.Common.Interfaces;

public interface IECFRApiClient
{
    Task<IReadOnlyList<EcfrTitleDto>> GetTitlesAsync(CancellationToken ct = default);
    Task<EcfrStructureNodeDto> GetTitleStructureAsync(int titleNumber, DateOnly date, CancellationToken ct = default);
    Task<EcfrVersionsResponse> GetVersionsSinceAsync(int titleNumber, DateOnly sinceDate, CancellationToken ct = default);
    Task<string> GetSectionContentHtmlAsync(DateOnly date, string sectionPath, CancellationToken ct = default);
    Task<string> GetCurrentSectionContentHtmlAsync(int titleNumber, int partNumber, string sectionIdentifier, CancellationToken ct = default);
}
