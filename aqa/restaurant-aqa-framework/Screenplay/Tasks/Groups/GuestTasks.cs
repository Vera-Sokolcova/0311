using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Screenplay.Questions.Groups;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.Groups;

/// <summary>A guest accepts the invitation of the owner's group. When it works, the guest takes a seat in the cast.</summary>
public sealed class JoinTheGroup(string guest, string owner) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var group = actor.TheCast().GroupOf(owner);
        var response = actor.TheLastResponse();

        await response.CaptureAsync(await new GroupsApi(actor.UsingTheApi())
            .CreateGuestSessionAsync(new CreateGuestSessionRequest(group.GroupId, guest)));
        if (response.Succeeded)
        {
            var session = response.Json<GuestSessionResponse>();
            actor.TheCast().Add(new GuestSeat(guest, session.SessionId, session.SessionToken, group.GroupId));
        }
    }
}

/// <summary>A guest puts portions of a dish (by its place on the menu) into their cart.</summary>
public sealed class PutIntoTheCart(string guest, int dishNumber, int quantity) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var seat = actor.TheCast().Guest(guest);
        var menu = await actor.AsksForAsync(new TheMenuOfTheGroup());

        await actor.TheLastResponse().CaptureAsync(await new GroupsApi(actor.UsingTheApi())
            .AddDishAsync(seat.SessionId, new GuestDishRequest(menu[dishNumber].Id, quantity), seat.SessionToken));
    }
}

/// <summary>A guest takes portions of a dish out of their cart again.</summary>
public sealed class TakeOutOfTheCart(string guest, int dishNumber, int quantity) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var seat = actor.TheCast().Guest(guest);
        var menu = await actor.AsksForAsync(new TheMenuOfTheGroup());

        await actor.TheLastResponse().CaptureAsync(await new GroupsApi(actor.UsingTheApi())
            .RemoveDishAsync(seat.SessionId, new GuestDishRequest(menu[dishNumber].Id, quantity), seat.SessionToken));
    }
}

/// <summary>A guest confirms their cart, handing it over to the owner.</summary>
public sealed class ConfirmTheCart(string guest) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var seat = actor.TheCast().Guest(guest);
        await actor.TheLastResponse().CaptureAsync(
            await new GroupsApi(actor.UsingTheApi()).SubmitCartAsync(seat.SessionId, seat.SessionToken));
    }
}
