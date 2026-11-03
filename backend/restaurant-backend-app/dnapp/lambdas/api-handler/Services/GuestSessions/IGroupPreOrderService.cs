using APIHandler.Models.GuestSessions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Services.GuestSessions;

/// <summary>Owner-only operations over the group pre-order.</summary>
public interface IGroupPreOrderService
{
    /// <summary>Returns every participant of the group with dishes, per-participant totals and the group total.</summary>
    Task<GetAllSessionsResponse> GetGroupSessionsAsync(string userId, string groupReservationId);

    /// <summary>Returns (and lazily creates) the owner's own guest session for the reservation.</summary>
    Task<OwnerGuestSessionResponse> GetOwnerGuestSessionIdAsync(string userId, string reservationId);

    /// <summary>
    /// Merges dishes of all submitted sessions by dishId, adds them to the order,
    /// sets order status to SubmittedByCustomer and group status to Submitted.
    /// </summary>
    Task<IReadOnlyList<ConsolidatedDishResponse>> SubmitGroupPreOrderAsync(string userId, string groupReservationId, string reservationId, string? comment = null);
}
