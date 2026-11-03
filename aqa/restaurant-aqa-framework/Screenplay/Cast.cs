using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay;

/// <summary>A customer account the scenario registered and signed in.</summary>
public sealed record Person(string Name, string Email, string Password, string IdToken);

/// <summary>A booked table with its group: the ids every later call needs.</summary>
public sealed record GroupBooking(string ReservationId, string GroupId, string InviteToken, string LocationId);

/// <summary>A guest who joined a group, with the token that authorises their calls.</summary>
public sealed record GuestSeat(string Name, string SessionId, string SessionToken, string GroupId);

/// <summary>The people of one scenario, by the name the feature file gives them. Kept in the actor's memory.</summary>
public sealed class Cast
{
    private readonly Dictionary<string, Person> _customers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GuestSeat> _guests = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GroupBooking> _groups = new(StringComparer.Ordinal);

    /// <summary>The menu of the groups' location, read once per scenario.</summary>
    public IReadOnlyList<DishListResponse>? Menu { get; set; }

    /// <summary>All groups of a scenario sit at the same configured test table, hence at one location.</summary>
    public string LocationOfTheGroups() =>
        _groups.Values.FirstOrDefault()?.LocationId
        ?? throw new InvalidOperationException("No table has been booked in this scenario yet.");

    public void Add(Person person) => _customers[person.Name] = person;

    public void Add(GuestSeat seat) => _guests[seat.Name] = seat;

    public void AddGroupOf(string owner, GroupBooking group) => _groups[owner] = group;

    public Person Customer(string name) =>
        _customers.TryGetValue(name, out var person) ? person : throw Unknown("customer", name);

    public GuestSeat Guest(string name) =>
        _guests.TryGetValue(name, out var seat) ? seat : throw Unknown("guest", name);

    public GroupBooking GroupOf(string owner) =>
        _groups.TryGetValue(owner, out var group) ? group : throw Unknown("group of", owner);

    public string? BearerOfNamed(string name) =>
        _customers.TryGetValue(name, out var person) ? person.IdToken
        : _guests.TryGetValue(name, out var seat) ? seat.SessionToken
        : throw Unknown("person", name);

    private static InvalidOperationException Unknown(string what, string name) =>
        new($"The scenario has no {what} named '{name}'. Introduce them in an earlier step.");
}

public static class CastExtensions
{
    public static Cast TheCast(this Actor actor)
    {
        if (actor.TryRecall<Cast>(out var cast))
            return cast;

        cast = new Cast();
        actor.Remember(cast);
        return cast;
    }
}

/// <summary>Whoever looks at or changes a group: a named person, the waiter, or somebody not signed in.</summary>
public abstract record Viewer
{
    public static Viewer Named(string name) => new NamedViewer(name);

    public static Viewer Waiter { get; } = new WaiterViewer();

    public static Viewer Nobody { get; } = new NobodyViewer();

    public abstract Task<string?> BearerAsync(Actor actor);

    private sealed record NamedViewer(string Name) : Viewer
    {
        public override Task<string?> BearerAsync(Actor actor) => Task.FromResult(actor.TheCast().BearerOfNamed(Name));
    }

    private sealed record WaiterViewer : Viewer
    {
        public override async Task<string?> BearerAsync(Actor actor) =>
            (await actor.AsksForAsync(new TheWaiterSession())).IdToken;
    }

    private sealed record NobodyViewer : Viewer
    {
        public override Task<string?> BearerAsync(Actor actor) => Task.FromResult<string?>(null);
    }
}
