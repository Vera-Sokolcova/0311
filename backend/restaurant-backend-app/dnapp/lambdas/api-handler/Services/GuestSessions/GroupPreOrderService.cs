using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Mappers;
using APIHandler.Models.GuestSessions;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Dishes;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Services.GuestSessions;

public class GroupPreOrderService(
    IGroupReservationRepository groupReservationRepository,
    IGuestSessionRepository guestSessionRepository,
    IReservationRepository reservationRepository,
    IDishRepository dishRepository,
    IOrderRepository orderRepository,
    IUserRepository userRepository) : IGroupPreOrderService
{
    private const string DefaultOwnerName = "Owner";

    public async Task<GetAllSessionsResponse> GetGroupSessionsAsync(string userId, string groupReservationId)
    {
        GroupReservation group = await GetGroupAsync(groupReservationId);
        Reservation reservation = await GetReservationAsync(group.ReservationId);
        EnsureOwner(reservation, userId);

        List<GuestSession> sessions = await guestSessionRepository.GetAllByGroupReservationIdAsync(groupReservationId);
        Order? order = await orderRepository.GetOrderByReservationIdAsync(reservation.Id);
        List<OrderItem> ownerOrderItems = GetOwnerOrderItems(order, sessions, group.Status);
        var dishesById = await LoadDishesAsync(sessions, ownerOrderItems);
        string ownerName = await GetOwnerNameAsync(reservation);

        return GroupSnapshotMapper.ToSnapshot(sessions, dishesById, ownerName, ownerOrderItems);
    }

    public async Task<OwnerGuestSessionResponse> GetOwnerGuestSessionIdAsync(string userId, string reservationId)
    {
        Reservation reservation = await GetReservationAsync(reservationId);
        EnsureOwner(reservation, userId);

        GroupReservation group = await groupReservationRepository.GetByReservationIdAsync(reservationId)
            ?? throw new GroupReservationNotFoundException();

        List<GuestSession> sessions = await guestSessionRepository.GetAllByGroupReservationIdAsync(group.GroupReservationId);
        GuestSession? ownerSession = sessions.FirstOrDefault(s => s.IsOwnerSession);

        if (ownerSession is null)
        {
            ownerSession = new GuestSession
            {
                Id = Guid.NewGuid().ToString(),
                GroupReservationId = group.GroupReservationId,
                GuestName = DefaultOwnerName,
                Dishes = [],
                IsOwnerSession = true,
                OwnerUserId = userId,
            };
            await guestSessionRepository.SaveAsync(ownerSession);
        }

        return new OwnerGuestSessionResponse(ownerSession.Id);
    }

    public async Task<IReadOnlyList<ConsolidatedDishResponse>> SubmitGroupPreOrderAsync(
        string userId, string groupReservationId, string reservationId)
    {
        GroupReservation group = await GetGroupAsync(groupReservationId);
        Reservation reservation = await GetReservationAsync(reservationId);

        if (group.ReservationId != reservation.Id)
            throw new ValidationException("Group reservation does not belong to the given reservation.");

        EnsureOwner(reservation, userId);

        if (group.Status == GroupReservationStatus.Submitted)
            throw new GroupReservationSubmittedException();

        if (group.Status == GroupReservationStatus.Closed)
            throw new GroupReservationExpiredException();

        List<GuestSession> sessions = await guestSessionRepository.GetAllByGroupReservationIdAsync(groupReservationId);
        List<GuestSession> submitted = sessions.Where(s => s.IsSubmitted).ToList();

        if (submitted.Count == 0)
            throw new ValidationException("There are no submitted guest sessions in the group.");

        List<ConsolidatedDishResponse> consolidated = submitted
            .SelectMany(s => s.Dishes ?? [])
            .Where(d => d.Quantity > 0)
            .GroupBy(d => d.DishId)
            .Select(g => new ConsolidatedDishResponse(g.Key, g.Sum(d => d.Quantity)))
            .ToList();

        if (consolidated.Count == 0)
            throw new ValidationException("Submitted guest sessions do not contain any dishes.");

        Order order = await orderRepository.GetOrderByReservationIdAsync(reservationId)
            ?? throw new ValidationException("Order not found.");

        foreach (ConsolidatedDishResponse dish in consolidated)
        {
            OrderItem? existing = order.Dishes.FirstOrDefault(d => d.DishId == dish.DishId);
            if (existing is null)
                order.Dishes.Add(new OrderItem { DishId = dish.DishId, Quantity = dish.Quantity, State = OrderItemState.Pending });
            else
                existing.Quantity += dish.Quantity;
        }

        order.Status = OrderStatus.SubmittedByCustomer;
        await orderRepository.SaveOrderAsync(order);

        group.Status = GroupReservationStatus.Submitted;
        await groupReservationRepository.SaveGroupReservationAsync(group);

        return consolidated;
    }

    private async Task<GroupReservation> GetGroupAsync(string groupReservationId)
        => await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId)
            ?? throw new GroupReservationNotFoundException();

    private async Task<Reservation> GetReservationAsync(string reservationId)
        => await reservationRepository.GetReservationByIdAsync(reservationId)
            ?? throw new ReservationNotFoundException($"Reservation {reservationId} was not found.");

    private static void EnsureOwner(Reservation reservation, string userId)
    {
        if (!string.Equals(reservation.CustomerId, userId, StringComparison.OrdinalIgnoreCase))
            throw new ActionForbiddenException("Only the reservation owner can perform this action.");
    }

    /// <summary>
    /// The owner's own dishes: the reservation's regular order. After the group is submitted the order also
    /// holds the guests' dishes (they are merged into it), so those quantities are taken away again.
    /// </summary>
    private static List<OrderItem> GetOwnerOrderItems(Order? order, IEnumerable<GuestSession> sessions, GroupReservationStatus groupStatus)
    {
        if (order?.Dishes is null)
            return [];

        Dictionary<string, int> remaining = order.Dishes
            .Where(d => d.Quantity > 0)
            .GroupBy(d => d.DishId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Quantity));

        if (groupStatus == GroupReservationStatus.Submitted)
        {
            foreach (OrderItem guestDish in sessions.Where(s => !s.IsOwnerSession && s.IsSubmitted).SelectMany(s => s.Dishes ?? []))
            {
                if (remaining.TryGetValue(guestDish.DishId, out int quantity))
                    remaining[guestDish.DishId] = quantity - guestDish.Quantity;
            }
        }

        return remaining
            .Where(pair => pair.Value > 0)
            .Select(pair => new OrderItem { DishId = pair.Key, Quantity = pair.Value, State = OrderItemState.Pending })
            .ToList();
    }

    private async Task<Dictionary<string, Dish>> LoadDishesAsync(IEnumerable<GuestSession> sessions, IEnumerable<OrderItem> extraItems)
    {
        string[] dishIds = sessions
            .SelectMany(s => s.Dishes ?? [])
            .Concat(extraItems)
            .Select(d => d.DishId)
            .Distinct()
            .ToArray();

        if (dishIds.Length == 0)
            return new Dictionary<string, Dish>();

        IEnumerable<Dish> dishes = await dishRepository.GetDishesByIdsAsync(dishIds);
        return dishes.ToDictionary(d => d.Id);
    }

    private async Task<string> GetOwnerNameAsync(Reservation reservation)
    {
        if (string.IsNullOrWhiteSpace(reservation.CustomerId))
            return DefaultOwnerName;

        var user = await userRepository.GetUserByIdAsync(reservation.CustomerId);
        string? givenName = user?.Attributes.FirstOrDefault(a => a.Name == "given_name")?.Value;
        return string.IsNullOrWhiteSpace(givenName) ? DefaultOwnerName : givenName;
    }
}
