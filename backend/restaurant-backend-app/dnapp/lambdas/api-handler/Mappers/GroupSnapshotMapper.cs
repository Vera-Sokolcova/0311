using APIHandler.Entities;
using APIHandler.Models.GuestSessions;
using APIHandler.Models.Orders;
using System.Collections.Generic;
using System.Linq;

namespace APIHandler.Mappers;

public static class GroupSnapshotMapper
{
    /// <summary>
    /// Builds the owner-facing snapshot of a group: every participant with dishes, per-participant total
    /// and the overall total. The owner session (if any) always comes first.
    /// </summary>
    public static GetAllSessionsResponse ToSnapshot(
        IEnumerable<GuestSession> sessions,
        IReadOnlyDictionary<string, Dish> dishesById,
        string ownerName,
        IReadOnlyCollection<OrderItem>? ownerOrderItems = null)
    {
        var participants = sessions
            .OrderByDescending(s => s.IsOwnerSession)
            .Select(s => ToParticipant(s, dishesById, ownerName, s.IsOwnerSession ? ownerOrderItems : null))
            .ToList();

        return new GetAllSessionsResponse(participants, participants.Sum(p => p.Total));
    }

    private static Participant ToParticipant(
        GuestSession session,
        IReadOnlyDictionary<string, Dish> dishesById,
        string ownerName,
        IReadOnlyCollection<OrderItem>? ownerOrderItems)
    {
        // The owner's own pre-order (the regular order of the reservation) is part of the owner's cart.
        var ownerDishes = (session.Dishes ?? [])
            .Concat(ownerOrderItems ?? [])
            .GroupBy(d => d.DishId)
            .Select(g => new OrderItem { DishId = g.Key, Quantity = g.Sum(d => d.Quantity), State = g.First().State })
            .ToList();

        var items = ownerDishes
            .Where(d => dishesById.ContainsKey(d.DishId))
            .Select(d => new OrderItemResponse
            {
                Dish = DishMapper.ToDishResponse(dishesById[d.DishId]),
                Quantity = d.Quantity
            })
            .ToArray();

        decimal total = items.Sum(i => i.Quantity * i.Dish.Price);

        return new Participant(
            session.Id,
            session.IsOwnerSession ? ParticipantType.Owner : ParticipantType.Guest,
            session.IsOwnerSession ? ownerName : session.GuestName,
            items,
            total,
            session.IsSubmitted || ownerOrderItems is { Count: > 0 });
    }
}
