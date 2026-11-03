using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.GuestSessions;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Dishes;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using APIHandler.Services.GuestSessions;
using Moq;

namespace APIHandler.Tests.Services;

public class GroupPreOrderServiceTests
{
    private const string OwnerId = "owner-1";
    private const string GroupId = "group-1";
    private const string ReservationId = "reservation-1";

    private readonly Mock<IGroupReservationRepository> _groups = new();
    private readonly Mock<IGuestSessionRepository> _sessions = new();
    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Mock<IDishRepository> _dishes = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly GroupPreOrderService _service;

    private readonly GroupReservation _group = new()
    {
        GroupReservationId = GroupId,
        ReservationId = ReservationId,
        ReservationToken = "token",
        Status = GroupReservationStatus.Open,
    };

    public GroupPreOrderServiceTests()
    {
        _service = new GroupPreOrderService(
            _groups.Object, _sessions.Object, _reservations.Object, _dishes.Object, _orders.Object, _users.Object);

        _groups.Setup(r => r.GetGroupReservationByIdAsync(GroupId)).ReturnsAsync(_group);
        _groups.Setup(r => r.GetByReservationIdAsync(ReservationId)).ReturnsAsync(_group);
        _reservations.Setup(r => r.GetReservationByIdAsync(ReservationId))
            .ReturnsAsync(new Reservation { Id = ReservationId, TableId = "t1", WaiterId = "w1", CustomerId = OwnerId });
    }

    private static Dish MakeDish(string id, decimal price) => new() { Id = id, Name = id, Price = price };

    private static GuestSession MakeSession(string id, bool submitted, params (string dish, int qty)[] items) => new()
    {
        Id = id,
        GuestName = id,
        GroupReservationId = GroupId,
        IsSubmitted = submitted,
        Dishes = items.Select(i => new OrderItem { DishId = i.dish, Quantity = i.qty }).ToList(),
    };

    private void SetupSessions(params GuestSession[] sessions)
    {
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync(sessions.ToList());
        _dishes.Setup(r => r.GetDishesByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync([MakeDish("dish1", 180), MakeDish("dish2", 220)]);
    }

    [Fact]
    public async Task GetGroupSessions_ReturnsParticipantsWithTotals()
    {
        SetupSessions(
            MakeSession("s1", true, ("dish1", 2)),
            MakeSession("s2", false, ("dish2", 1)));

        var result = await _service.GetGroupSessionsAsync(OwnerId, GroupId);

        Assert.Equal(2, result.Participants.Count());
        Assert.Equal(360, result.Participants.First(p => p.Id == "s1").Total);
        Assert.True(result.Participants.First(p => p.Id == "s1").IsSubmitted);
        Assert.Equal(220, result.Participants.First(p => p.Id == "s2").Total);
        Assert.Equal(580, result.Total);
    }

    [Fact]
    public async Task GetGroupSessions_EmptyGroup_ReturnsEmptyList()
    {
        SetupSessions();

        var result = await _service.GetGroupSessionsAsync(OwnerId, GroupId);

        Assert.Empty(result.Participants);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetGroupSessions_OwnerSessionComesFirstAndIsTypedOwner()
    {
        var owner = MakeSession("own", false, ("dish1", 1));
        owner.IsOwnerSession = true;
        SetupSessions(MakeSession("s1", true, ("dish2", 1)), owner);

        var result = await _service.GetGroupSessionsAsync(OwnerId, GroupId);

        Assert.Equal(ParticipantType.Owner, result.Participants.First().Type);
        Assert.Equal("Owner", result.Participants.First().Name);
    }

    [Fact]
    public async Task GetGroupSessions_NotOwner_Throws()
    {
        SetupSessions();
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.GetGroupSessionsAsync("someone-else", GroupId));
    }

    [Fact]
    public async Task GetGroupSessions_UnknownGroup_Throws()
    {
        _groups.Setup(r => r.GetGroupReservationByIdAsync("nope")).ReturnsAsync((GroupReservation?)null);
        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => _service.GetGroupSessionsAsync(OwnerId, "nope"));
    }

    [Fact]
    public async Task GetOwnerGuestSessionId_CreatesSessionWhenMissing()
    {
        SetupSessions(MakeSession("s1", true));
        GuestSession? saved = null;
        _sessions.Setup(r => r.SaveAsync(It.IsAny<GuestSession>())).Callback<GuestSession>(s => saved = s).Returns(Task.CompletedTask);

        var result = await _service.GetOwnerGuestSessionIdAsync(OwnerId, ReservationId);

        Assert.NotNull(saved);
        Assert.True(saved!.IsOwnerSession);
        Assert.Equal(OwnerId, saved.OwnerUserId);
        Assert.Equal(saved.Id, result.GuestSessionId);
    }

    [Fact]
    public async Task GetOwnerGuestSessionId_ReusesExistingSession()
    {
        var owner = MakeSession("own", false);
        owner.IsOwnerSession = true;
        SetupSessions(owner);

        var result = await _service.GetOwnerGuestSessionIdAsync(OwnerId, ReservationId);

        Assert.Equal("own", result.GuestSessionId);
        _sessions.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task GetOwnerGuestSessionId_NotOwner_Throws()
    {
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.GetOwnerGuestSessionIdAsync("x", ReservationId));
    }

    [Fact]
    public async Task Submit_MergesDishesAcrossSubmittedSessionsOnly_AndUpdatesStatuses()
    {
        SetupSessions(
            MakeSession("s1", true, ("dish1", 2), ("dish2", 1)),
            MakeSession("s2", true, ("dish1", 1), ("dish2", 1)),
            MakeSession("s3", false, ("dish1", 10)));
        var order = new Order
        {
            OrderId = "o1", CustomerId = OwnerId, ReservationId = ReservationId,
            ReservationAddress = "a", ReservationTime = "t", Dishes = [], Status = OrderStatus.Created,
        };
        _orders.Setup(r => r.GetOrderByReservationIdAsync(ReservationId)).ReturnsAsync(order);

        var result = await _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId);

        Assert.Equal(3, result.Single(d => d.DishId == "dish1").Quantity);
        Assert.Equal(2, result.Single(d => d.DishId == "dish2").Quantity);
        Assert.Equal(OrderStatus.SubmittedByCustomer, order.Status);
        Assert.Equal(GroupReservationStatus.Submitted, _group.Status);
        Assert.Equal(3, order.Dishes.Single(d => d.DishId == "dish1").Quantity);
        _orders.Verify(r => r.SaveOrderAsync(order), Times.Once);
        _groups.Verify(r => r.SaveGroupReservationAsync(_group), Times.Once);
    }

    [Fact]
    public async Task Submit_NoSubmittedSessions_ThrowsValidation()
    {
        SetupSessions(MakeSession("s1", false, ("dish1", 1)));
        await Assert.ThrowsAsync<ValidationException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));
    }

    [Fact]
    public async Task Submit_SubmittedSessionsWithoutDishes_ThrowsValidation()
    {
        SetupSessions(MakeSession("s1", true));
        await Assert.ThrowsAsync<ValidationException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));
    }

    [Fact]
    public async Task Submit_OrderMissing_ThrowsValidation()
    {
        SetupSessions(MakeSession("s1", true, ("dish1", 1)));
        _orders.Setup(r => r.GetOrderByReservationIdAsync(ReservationId)).ReturnsAsync((Order?)null);
        await Assert.ThrowsAsync<ValidationException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));
        _groups.Verify(r => r.SaveGroupReservationAsync(It.IsAny<GroupReservation>()), Times.Never);
    }

    [Fact]
    public async Task Submit_AlreadySubmitted_Throws()
    {
        _group.Status = GroupReservationStatus.Submitted;
        SetupSessions(MakeSession("s1", true, ("dish1", 1)));
        await Assert.ThrowsAsync<GroupReservationSubmittedException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));
    }

    [Fact]
    public async Task Submit_NotOwner_Throws()
    {
        SetupSessions(MakeSession("s1", true, ("dish1", 1)));
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.SubmitGroupPreOrderAsync("x", GroupId, ReservationId));
    }

    [Fact]
    public async Task Submit_UnknownReservation_ThrowsNotFound()
    {
        _reservations.Setup(r => r.GetReservationByIdAsync("nope")).ReturnsAsync((Reservation?)null);
        await Assert.ThrowsAsync<ReservationNotFoundException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, "nope"));
    }
}
