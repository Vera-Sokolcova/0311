using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.Groups;

/// <summary>Opens the list of participants of the owner's group as <paramref name="who"/> would.</summary>
public sealed class ViewTheGroup(Viewer who, string owner) : ITask
{
    public async Task PerformAsAsync(Actor actor) =>
        await actor.TheLastResponse().CaptureAsync(await new GroupsApi(actor.UsingTheApi())
            .GetGroupSessionsAsync(actor.TheCast().GroupOf(owner).GroupId, await who.BearerAsync(actor)));
}

/// <summary>Opens a group that does not exist.</summary>
public sealed class ViewAGroupThatDoesNotExist(Viewer who) : ITask
{
    public async Task PerformAsAsync(Actor actor) =>
        await actor.TheLastResponse().CaptureAsync(await new GroupsApi(actor.UsingTheApi())
            .GetGroupSessionsAsync("does-not-exist", await who.BearerAsync(actor)));
}

/// <summary>Confirms the owner's group pre-order, handing the consolidated order to the restaurant.</summary>
public sealed class ConfirmTheGroupPreOrder(Viewer who, string owner) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var booking = actor.TheCast().GroupOf(owner);
        await actor.TheLastResponse().CaptureAsync(await new GroupsApi(actor.UsingTheApi())
            .SubmitGroupAsync(booking.GroupId, booking.ReservationId, await who.BearerAsync(actor)));
    }
}

/// <summary>Confirms a group pre-order for a group that does not exist.</summary>
public sealed class ConfirmAGroupThatDoesNotExist(Viewer who) : ITask
{
    public async Task PerformAsAsync(Actor actor) =>
        await actor.TheLastResponse().CaptureAsync(await new GroupsApi(actor.UsingTheApi())
            .SubmitGroupAsync("does-not-exist", "does-not-exist", await who.BearerAsync(actor)));
}

/// <summary>Removes a guest from the group of <paramref name="owner"/> (the guest may belong to another group).</summary>
public sealed class RemoveTheParticipant(Viewer who, string owner, string participant) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var cast = actor.TheCast();
        await actor.TheLastResponse().CaptureAsync(await new GroupsApi(actor.UsingTheApi())
            .DeleteParticipantAsync(cast.GroupOf(owner).GroupId, cast.Guest(participant).SessionId, await who.BearerAsync(actor)));
    }
}

/// <summary>Removes somebody who never joined.</summary>
public sealed class RemoveAParticipantWhoDoesNotExist(Viewer who, string owner) : ITask
{
    public async Task PerformAsAsync(Actor actor) =>
        await actor.TheLastResponse().CaptureAsync(await new GroupsApi(actor.UsingTheApi())
            .DeleteParticipantAsync(actor.TheCast().GroupOf(owner).GroupId, "does-not-exist", await who.BearerAsync(actor)));
}

/// <summary>The owner's own seat in the group (created the first time it is asked for).</summary>
public sealed class TakeTheOwnersSeat(string owner) : ITask
{
    public async Task PerformAsAsync(Actor actor) =>
        await actor.TheLastResponse().CaptureAsync(await new GroupsApi(actor.UsingTheApi())
            .GetOwnerSessionIdAsync(actor.TheCast().GroupOf(owner).ReservationId, actor.TheCast().Customer(owner).IdToken));
}
