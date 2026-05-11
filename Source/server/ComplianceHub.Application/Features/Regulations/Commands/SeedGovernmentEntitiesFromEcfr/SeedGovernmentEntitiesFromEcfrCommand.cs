using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities.Regulations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Regulations.Commands.SeedGovernmentEntitiesFromEcfr;

public record SeedGovernmentEntitiesFromEcfrCommand : IRequest<int>;

public class SeedGovernmentEntitiesFromEcfrCommandHandler(
    IRegulationsUnitOfWork uow,
    IECFRApiClient ecfrClient,
    ICurrentUserService currentUser)
    : IRequestHandler<SeedGovernmentEntitiesFromEcfrCommand, int>
{
    public async Task<int> Handle(SeedGovernmentEntitiesFromEcfrCommand request, CancellationToken ct)
    {
        var existingTitleNumbers = await uow.GovernmentEntities.Query()
            .Select(e => e.TitleNumber)
            .ToHashSetAsync(ct);

        var titles = await ecfrClient.GetTitlesAsync(ct);

        var newTitles = titles.Where(t => !existingTitleNumbers.Contains(t.Number)).ToList();

        foreach (var title in newTitles)
        {
            await uow.GovernmentEntities.AddAsync(new GovernmentEntity
            {
                Identifier = title.Number.ToString(),
                TitleNumber = title.Number,
                TitleName = title.Name,
                Source = "eCFR",
                IsSyncEnabled = false,
                LastAmendedDate = title.LatestAmendedOn ?? title.LatestIssueDate,
                CreatedBy = currentUser.Email
            }, ct);
        }

        if (newTitles.Count > 0)
            await uow.SaveChangesAsync(ct);

        return newTitles.Count;
    }
}
