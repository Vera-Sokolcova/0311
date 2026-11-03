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
        string ownerName)
    {
        var participants = sessions
            .OrderByDescending(s => s.IsOwnerSession)
            .Select(s => ToParticipant(s, dishesById, ownerName))
            .ToList();

        return new GetAllSessionsResponse(participants, participants.Sum(p => p.Total));
    }

    private static Participant ToParticipant(
        GuestSession session,
        IReadOnlyDictionary<string, Dish> dishesById,
        string ownerName)
    {
        var items = (session.Dishes ?? [])
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
            session.IsSubmitted);
    }
}
