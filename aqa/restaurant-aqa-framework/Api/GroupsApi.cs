using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;

namespace Restaurant_Aqa_Framework.Api;

/// <summary>Invitations, guest sessions and the owner's group pre-order. Guest calls bear the guest's session
/// token; owner and waiter calls bear the account's id token.</summary>
public sealed class GroupsApi(IAPIRequestContext api) : ApiClientBase(api)
{
    public Task<IAPIResponse> GetInviteTokenAsync(string reservationId, string? bearerToken = null) =>
        GetAsync(Url($"/reservations/{reservationId}/invite-token"), new APIRequestContextOptions { Headers = Headers(bearerToken) });

    public Task<IAPIResponse> GetInviteAsync(string token) =>
        GetAsync(Url($"/guest/invites/{token}"), new APIRequestContextOptions { Headers = Headers() });

    public Task<IAPIResponse> GetOwnerSessionIdAsync(string reservationId, string? bearerToken = null) =>
        GetAsync(Url($"/reservations/{reservationId}/guest-session-id"), new APIRequestContextOptions { Headers = Headers(bearerToken) });

    public Task<IAPIResponse> CreateGuestSessionAsync(CreateGuestSessionRequest request) =>
        Api.PostAsync(Url("/guest/sessions"), new APIRequestContextOptions
        {
            DataString = Serialize(request),
            Headers = Headers(),
        });

    public Task<IAPIResponse> AddDishAsync(string sessionId, GuestDishRequest request, string? sessionToken) =>
        Api.PostAsync(Url($"/guest/sessions/{sessionId}/dishes"), new APIRequestContextOptions
        {
            DataString = Serialize(request),
            Headers = Headers(sessionToken),
        });

    public Task<IAPIResponse> RemoveDishAsync(string sessionId, GuestDishRequest request, string? sessionToken) =>
        DeleteAsync(Url($"/guest/sessions/{sessionId}/dishes"), new APIRequestContextOptions
        {
            DataString = Serialize(request),
            Headers = Headers(sessionToken),
        });

    public Task<IAPIResponse> SubmitCartAsync(string sessionId, string? sessionToken) =>
        Api.PostAsync(Url($"/guest/sessions/{sessionId}/submit"), new APIRequestContextOptions
        {
            Headers = Headers(sessionToken),
        });

    public Task<IAPIResponse> GetGroupSessionsAsync(string groupId, string? bearerToken = null) =>
        GetAsync(Url($"/guest/groups/{groupId}/sessions"), new APIRequestContextOptions { Headers = Headers(bearerToken) });

    public Task<IAPIResponse> SubmitGroupAsync(string groupId, string reservationId, string? bearerToken = null) =>
        Api.PostAsync(Url($"/group-reservations/{groupId}/submit"), new APIRequestContextOptions
        {
            DataString = Serialize(new SubmitGroupRequest(reservationId)),
            Headers = Headers(bearerToken),
        });

    public Task<IAPIResponse> DeleteParticipantAsync(string groupId, string sessionId, string? bearerToken = null) =>
        DeleteAsync(Url($"/guest/groups/{groupId}/sessions/{sessionId}"), new APIRequestContextOptions
        {
            Headers = Headers(bearerToken),
        });
}
