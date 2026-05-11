using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Regulations.Queries.GetSectionContent;

public record GetSectionContentQuery(Guid SectionId) : IRequest<string?>;

public class GetSectionContentQueryHandler(IRegulationsUnitOfWork uow)
    : IRequestHandler<GetSectionContentQuery, string?>
{
    public async Task<string?> Handle(GetSectionContentQuery request, CancellationToken ct)
    {
        return await uow.Regulations.Query()
            .IgnoreQueryFilters()
            .Where(r => r.Id == request.SectionId)
            .Select(r => r.HtmlContent)
            .FirstOrDefaultAsync(ct);
    }
}
