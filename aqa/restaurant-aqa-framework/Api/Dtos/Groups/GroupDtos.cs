using Restaurant_Aqa_Framework.Api.Dtos.Orders;

namespace Restaurant_Aqa_Framework.Api.Dtos.Groups;

/// <summary>GET /reservations/{id}/invite-token.</summary>
public sealed record InviteTokenResponse(string Token);

/// <summary>GET /guest/invites/{token} — what an invited guest sees; its id is the group id used everywhere else.</summary>
public sealed record GuestInviteResponse(
    string GroupReservationId,
    string LocationId,
    string RestaurantName,
    string LocationAddress,
    DateTime StartTime,
    DateTime EndTime,
    int MaxGuestCount,
    int TableNumber,
    string InviterName);

public sealed record CreateGuestSessionRequest(string GroupReservationId, string GuestName);

/// <summary>POST /guest/sessions. The token authorises every later call of that guest.</summary>
public sealed record GuestSessionResponse(string SessionId, string SessionToken);

/// <summary>Body of adding a dish to / removing a dish from a guest's cart.</summary>
public sealed record GuestDishRequest(string DishId, int Quantity);

/// <summary>GET /reservations/{id}/guest-session-id.</summary>
public sealed record OwnerGuestSessionResponse(string GuestSessionId);

/// <summary>One line of the owner's group view; <paramref name="Type"/> is "Owner" or "Guest".</summary>
public sealed record GroupParticipantResponse(
    string? Id,
    string Type,
    string Name,
    IReadOnlyList<OrderItemResponse> Items,
    decimal Total,
    bool IsSubmitted);

/// <summary>GET /guest/groups/{id}/sessions, and the answer to removing a participant.</summary>
public sealed record GroupSessionsResponse(IReadOnlyList<GroupParticipantResponse> Participants, decimal Total);

public sealed record SubmitGroupRequest(string ReservationId);

/// <summary>One dish of the consolidated list the owner's confirmation returns.</summary>
public sealed record ConsolidatedDishResponse(string DishId, int Quantity);
