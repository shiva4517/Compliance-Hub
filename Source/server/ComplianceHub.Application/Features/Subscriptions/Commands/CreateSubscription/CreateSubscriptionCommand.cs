using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Subscriptions.Commands.CreateSubscription;

public record SubscriptionItemRequest(
    Guid GovernmentEntityId,
    Guid? AgencyId,
    Guid? RegulationCategoryId,
    Guid? RegulationTypeId,
    Guid? RegulationSubtypeId,
    Guid? RegulationId,
    SubscribingLevel SubscribingLevel,
    string SubscribedNodeName);

public record CreateSubscriptionsCommand(Guid CustomerId, List<SubscriptionItemRequest> Items)
    : IRequest<CreateSubscriptionsResult>;

public record CreateSubscriptionsResult(int Saved, List<string> Duplicates);

public class CreateSubscriptionsCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IEmailService emailService)
    : IRequestHandler<CreateSubscriptionsCommand, CreateSubscriptionsResult>
{
    public async Task<CreateSubscriptionsResult> Handle(CreateSubscriptionsCommand request, CancellationToken ct)
    {
        var duplicates = new List<string>();
        var savedItems = new List<SubscriptionItemRequest>();

        foreach (var item in request.Items)
        {
            var exists = await uow.Subscriptions.Query()
                .IgnoreQueryFilters()
                .AnyAsync(s =>
                    !s.IsDeleted &&
                    s.CustomerId == request.CustomerId &&
                    s.GovernmentEntityId == item.GovernmentEntityId &&
                    s.AgencyId == item.AgencyId &&
                    s.RegulationCategoryId == item.RegulationCategoryId &&
                    s.RegulationTypeId == item.RegulationTypeId &&
                    s.RegulationSubtypeId == item.RegulationSubtypeId &&
                    s.RegulationId == item.RegulationId, ct);

            if (exists)
            {
                duplicates.Add($"Customer is already subscribed to '{item.SubscribedNodeName}'");
                continue;
            }

            var subscription = new Subscription
            {
                CustomerId = request.CustomerId,
                GovernmentEntityId = item.GovernmentEntityId,
                AgencyId = item.AgencyId,
                RegulationCategoryId = item.RegulationCategoryId,
                RegulationTypeId = item.RegulationTypeId,
                RegulationSubtypeId = item.RegulationSubtypeId,
                RegulationId = item.RegulationId,
                SubscribingLevel = item.SubscribingLevel,
                SubscribedNodeName = item.SubscribedNodeName,
                IsActive = true,
                CreatedBy = currentUser.Email
            };

            await uow.Subscriptions.AddAsync(subscription, ct);
            savedItems.Add(item);
        }

        if (savedItems.Count > 0)
        {
            await uow.SaveChangesAsync(ct);
            await SendConfirmationEmailAsync(request.CustomerId, savedItems, ct);
        }

        return new CreateSubscriptionsResult(savedItems.Count, duplicates);
    }

    private async Task SendConfirmationEmailAsync(Guid customerId, List<SubscriptionItemRequest> items, CancellationToken ct)
    {
        try
        {
            var customer = await uow.Customers.GetByIdAsync(customerId,ct);
            if (customer is null) return;

            var nodes = items.Select(i => (i.SubscribedNodeName, i.SubscribingLevel.ToString()));
            var (subject, body) = emailService.BuildSubscriptionConfirmationEmail(
                customer.CustomerName, customer.CustomerName, nodes);
            await emailService.SendRawEmailAsync(customer.PrimaryEmail, subject, body, CancellationToken.None);
        }
        catch (Exception ex)
        {

        }
    }
}
