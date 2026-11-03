using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Locations;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Configuration;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Questions.Groups;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.Groups;

/// <summary>Registers a new customer through the service, signs them in and adds them to the cast under <paramref name="name"/>.</summary>
public sealed class BecomeACustomer(string name) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var auth = new AuthApi(actor.UsingTheApi());
        var request = SampleDataFactory.RegisterRequest();
        var response = actor.TheLastResponse();

        await response.CaptureAsync(await auth.RegisterAsync(request));
        if (!response.Succeeded)
            throw new InvalidOperationException($"Registering {name} failed with {response.Status}: {response.Body}");

        await response.CaptureAsync(await auth.SignInAsync(new SignInRequest(request.Email, request.Password)));
        actor.TheCast().Add(new Person(name, request.Email, request.Password, response.Json<SignInResponse>().IdToken));
    }
}

/// <summary>Books the configured test table for a group (owner + guests) at a free slot far enough ahead, finds the
/// group's invite and registers the reservation for cleanup. A taken slot is retried with another one.</summary>
public sealed class BookATableForAGroup(string owner, int guests = 4) : ITask
{
    private const int Attempts = 6;

    public async Task PerformAsAsync(Actor actor)
    {
        var person = actor.TheCast().Customer(owner);
        var tableId = Settings.Current.TestTableId
            ?? throw new InvalidOperationException("TESTTABLE_ID is not set: group scenarios book that table. See '.env.example'.");
        var reservations = new ReservationsApi(actor.UsingTheApi());
        var response = actor.TheLastResponse();

        for (var attempt = 1; ; attempt++)
        {
            await response.CaptureAsync(await reservations.CreateReservationAsync(
                SampleDataFactory.CreateReservationRequest(tableId, SampleDataFactory.FutureSlotStart(), guests), person.IdToken));
            if (response.Succeeded)
                break;

            if (attempt == Attempts)
                throw new InvalidOperationException($"Booking the table failed {Attempts} times, last: {response.Status} {response.Body}");
        }

        var reservation = response.Json<ReservationResponse>();
        actor.Recall<CleanupScope>().TrackDelete(
            $"reservation:{reservation.Id}", () => reservations.CancelReservationAsync(reservation.Id, person.IdToken));

        var groups = new GroupsApi(actor.UsingTheApi());
        await response.CaptureAsync(await groups.GetInviteTokenAsync(reservation.Id, person.IdToken));
        var token = response.Json<InviteTokenResponse>().Token;

        await response.CaptureAsync(await groups.GetInviteAsync(token));
        var invite = response.Json<GuestInviteResponse>();

        await response.CaptureAsync(await new LocationsApi(actor.UsingTheApi()).GetLocationsAsync());
        var location = response.Json<List<LocationResponse>>()
            .First(l => string.Equals(l.Address, reservation.LocationAddress, StringComparison.Ordinal));

        actor.TheCast().AddGroupOf(owner, new GroupBooking(reservation.Id, invite.GroupReservationId, token, location.Id));
    }
}

/// <summary>The owner's own pre-order for the reservation: <paramref name="quantity"/> portions of one dish of the menu.</summary>
public sealed class PreOrderForTheTable(string owner, int dishNumber, int quantity) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var person = actor.TheCast().Customer(owner);
        var booking = actor.TheCast().GroupOf(owner);
        var menu = await actor.AsksForAsync(new TheMenuOfTheGroup());

        await actor.TheLastResponse().CaptureAsync(await new OrdersApi(actor.UsingTheApi()).CreateOrderAsync(
            new CreateOrderRequest(booking.ReservationId, [new OrderItemRequest(menu[dishNumber].Id, quantity)]),
            person.IdToken));
    }
}
