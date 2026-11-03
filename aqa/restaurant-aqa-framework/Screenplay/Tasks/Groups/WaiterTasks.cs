using Restaurant_Aqa_Framework.Api;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.Groups;

/// <summary>The waiter opens the pre-order of the owner's reservation.</summary>
public sealed class OpenThePreOrderAsTheWaiter(string owner) : ITask
{
    public async Task PerformAsAsync(Actor actor) =>
        await actor.TheLastResponse().CaptureAsync(await new OrdersApi(actor.UsingTheApi()).GetOrderAsync(
            actor.TheCast().GroupOf(owner).ReservationId, await Viewer.Waiter.BearerAsync(actor)));
}
