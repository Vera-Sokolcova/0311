using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CognitoIdentityProvider.Model;
using APIHandler.Entities;
using APIHandler.Entities.Reports;
using APIHandler.Models.Feedbacks;
using APIHandler.Models.WaiterList;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Dishes;
using APIHandler.Repositories.Feedbacks;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Locations;
using APIHandler.Repositories.Notifications;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reports;
using APIHandler.Repositories.Reservations;
using APIHandler.Repositories.Tables;
using APIHandler.Repositories.Waiters;

namespace APIHandler.Local;

internal static class T
{
    public static Task<X> R<X>(X value) => Task.FromResult(value);
    public static Task Done => Task.CompletedTask;
}

public sealed class InMemoryUserRepository(LocalAuth auth) : IUserRepository
{
    public Task<AdminGetUserResponse> GetUserByEmailAsync(string email) => T.R(auth.GetUser(email));

    public Task<UserType?> GetUserByIdAsync(string id)
    {
        var u = auth.FindBySub(id);
        return T.R(u is null ? null : LocalAuth.ToUserType(u));
    }

    public Task<ListUsersResponse> GetUsersAsync() => T.R(new ListUsersResponse
        { Users = auth.All().Select(LocalAuth.ToUserType).ToList() });

    public Task EnsureGroupExistsAsync(string groupName) { auth.EnsureGroup(groupName); return T.Done; }

    public Task AddUserToGroupAsync(string email, string groupName)
    {
        auth.EnsureGroup(groupName);
        (auth.FindByEmail(email) ?? throw new UserNotFoundException("User not found")).Groups.Add(groupName);
        return T.Done;
    }

    public Task RemoveUserFromGroupAsync(string email, string groupName)
    {
        auth.FindByEmail(email)?.Groups.Remove(groupName);
        return T.Done;
    }
}

public sealed class InMemoryDishRepository(LocalDb db) : IDishRepository
{
    public Task<Dish?> GetDishByIdAsync(string dishId) => T.R(db.Dishes.GetValueOrDefault(dishId));

    public Task<IEnumerable<Dish>> GetDishesByIdsAsync(IEnumerable<string> dishIds) =>
        T.R<IEnumerable<Dish>>(dishIds.Distinct().Select(i => db.Dishes.GetValueOrDefault(i)).OfType<Dish>().ToList());

    // Same rule as the DynamoDB repository: the location's specialty dishes plus dishes bound to it,
    // and the whole menu when the location has neither.
    public Task<IEnumerable<Dish>> GetDishesByLocationIdAsync(string locationId)
    {
        var specialtyIds = db.Locations.GetValueOrDefault(locationId)?.SpecialtyDishIds ?? [];
        var dishes = db.Dishes.Values
            .Where(d => d.LocationId == locationId || specialtyIds.Contains(d.Id))
            .ToList();

        return T.R<IEnumerable<Dish>>(dishes.Count > 0 ? dishes : db.Dishes.Values.ToList());
    }

    public Task SaveDishAsync(Dish dish) { db.Dishes[dish.Id] = dish; return T.Done; }
    public Task DeleteDishAsync(Dish dish) { db.Dishes.TryRemove(dish.Id, out _); return T.Done; }
}

public sealed class InMemoryFeedbackRepository(LocalDb db) : IFeedbackRepository
{
    public Task<List<Feedback>> GetFeedbackByLocationIdAsync(string locationId) =>
        T.R(db.Feedbacks.Values.Where(f => f.LocationId == locationId).OrderByDescending(f => f.CreatedAt).ToList());

    public Task<List<Feedback>> GetFeedbackByReservationIdAsync(string reservationId) =>
        T.R(db.Feedbacks.Values.Where(f => f.ReservationId == reservationId).ToList());

    public Task SaveFeedbackAsync(Feedback feedback)
    {
        db.Feedbacks[$"{feedback.ReservationId}|{feedback.CreatedAt.Ticks}"] = feedback;
        return T.Done;
    }
}

public sealed class InMemoryGroupReservationRepository(LocalDb db) : IGroupReservationRepository
{
    public Task<GroupReservation?> GetByReservationIdAsync(string reservationId) =>
        T.R(db.GroupReservations.Values.FirstOrDefault(g => g.ReservationId == reservationId));

    public Task<GroupReservation?> GetGroupReservationByIdAsync(string groupReservationId) =>
        T.R(db.GroupReservations.GetValueOrDefault(groupReservationId));

    public Task DeleteGroupReservationAsync(GroupReservation g) { db.GroupReservations.TryRemove(g.GroupReservationId, out _); return T.Done; }
    public Task SaveGroupReservationAsync(GroupReservation g) { db.GroupReservations[g.GroupReservationId] = g; return T.Done; }

    public Task<GroupReservation?> GetGroupReservationByTokenAsync(string token) =>
        T.R(db.GroupReservations.Values.FirstOrDefault(g => g.ReservationToken == token));
}

public sealed class InMemoryGuestSessionRepository(LocalDb db) : IGuestSessionRepository
{
    public Task<int> CountByGroupReservationId(string groupReservationId) =>
        T.R(db.GuestSessions.Values.Count(s => s.GroupReservationId == groupReservationId && !s.IsOwnerSession));

    public Task<GuestSession?> GetByGroupReservationId(string groupReservationId) =>
        T.R(db.GuestSessions.Values.FirstOrDefault(s => s.GroupReservationId == groupReservationId));

    public Task<GuestSession?> GetByGuestSessionId(string id) => T.R(db.GuestSessions.GetValueOrDefault(id));

    public Task<GuestSession?> GetByTokenHash(string tokenHash) =>
        T.R(db.GuestSessions.Values.FirstOrDefault(s => s.SessionTokenHash == tokenHash));

    public Task<List<GuestSession>> GetAllByGroupReservationIdAsync(string groupReservationId) =>
        T.R(db.GuestSessions.Values.Where(s => s.GroupReservationId == groupReservationId).ToList());

    public Task SaveAsync(GuestSession s) { db.GuestSessions[s.Id] = s; return T.Done; }
    public Task DeleteAsync(GuestSession s) { db.GuestSessions.TryRemove(s.Id, out _); return T.Done; }
}

public sealed class InMemoryLocationRepository(LocalDb db) : ILocationRepository
{
    public Task<Location?> GetLocationByIdAsync(string locationId) => T.R(db.Locations.GetValueOrDefault(locationId));
    public Task<Table?> GetTableByIdAsync(string tableId) => T.R(db.Tables.GetValueOrDefault(tableId));

    public Task<List<Table>> GetTablesByLocationIdAsync(string locationId) =>
        T.R(db.Tables.Values.Where(t => t.LocationId == locationId).ToList());

    public Task<List<Feedback>> GetFeedbackByLocationIdAsync(string locationId) =>
        T.R(db.Feedbacks.Values.Where(f => f.LocationId == locationId).ToList());

    public Task<IEnumerable<Location>> GetAllLocationsAsync() => T.R<IEnumerable<Location>>(db.Locations.Values.ToList());
    public Task SaveLocationAsync(Location l) { db.Locations[l.Id] = l; return T.Done; }
    public Task DeleteLocationAsync(Location l) { db.Locations.TryRemove(l.Id, out _); return T.Done; }
}

public sealed class InMemoryNotificationRepository(LocalDb db) : INotificationRepository
{
    public Task<IEnumerable<Notification>> GetUnreadNotificationsByUserIdAsync(string userId) =>
        T.R<IEnumerable<Notification>>(db.Notifications.Values.Where(n => n.UserId == userId && !n.IsRead).ToList());

    public Task<Notification?> GetNotificationByIdAsync(string id) => T.R(db.Notifications.GetValueOrDefault(id));
    public Task SaveNotificationAsync(Notification n) { db.Notifications[n.Id] = n; return T.Done; }
}

public sealed class InMemoryOrderRepository(LocalDb db) : IOrderRepository
{
    public Task<Order?> GetOrderByIdAsync(string orderId, string customerId)
    {
        var o = db.Orders.GetValueOrDefault(orderId);
        return T.R(o is not null && o.CustomerId == customerId ? o : null);
    }

    public Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId) =>
        T.R<IEnumerable<Order>>(db.Orders.Values.Where(o => o.CustomerId == customerId).ToList());

    public Task<Order?> GetOrderByReservationIdAsync(string reservationId) =>
        T.R(db.Orders.Values.FirstOrDefault(o => o.ReservationId == reservationId));

    public Task SaveOrderAsync(Order o) { db.Orders[o.OrderId] = o; return T.Done; }
}

public sealed class InMemoryReportRepository : IReportRepository
{
    public Task<List<DailyAggregate>> GetAggregatesForPeriodAsync(DateTime s, DateTime e, string locationId) => T.R(new List<DailyAggregate>());
    public Task<List<ReportHistoryWaiter>> GetWaiterReportsByUserIdAsync(string userId) => T.R(new List<ReportHistoryWaiter>());
    public Task<List<ReportHistoryLocation>> GetLocationReportsByUserIdAsync(string userId) => T.R(new List<ReportHistoryLocation>());
    public Task SaveReportHistoryAsync<TReport>(TReport report) where TReport : ReportHistoryBase => T.Done;
}

public sealed class InMemoryReservationRepository(LocalDb db) : IReservationRepository
{
    public Task<Reservation?> GetReservationByIdAsync(string id) => T.R(db.Reservations.GetValueOrDefault(id));

    public Task<List<Reservation>> GetReservationsByTableIdAsync(string tableId) =>
        T.R(db.Reservations.Values.Where(r => r.TableId == tableId).ToList());

    public Task<IEnumerable<Reservation>> GetReservationsByWaiterIdAsync(string waiterId) =>
        T.R<IEnumerable<Reservation>>(db.Reservations.Values.Where(r => r.WaiterId == waiterId).ToList());

    public Task<IEnumerable<Reservation>> GetReservationsByCustomerIdAsync(string userId) =>
        T.R<IEnumerable<Reservation>>(db.Reservations.Values.Where(r => r.CustomerId == userId).ToList());

    public Task SaveReservationAsync(Reservation r) { db.Reservations[r.Id] = r; return T.Done; }
}

public sealed class InMemoryTableRepository(LocalDb db) : ITableRepository
{
    public Task<Table?> GetTableAsync(string id) => T.R(db.Tables.GetValueOrDefault(id));
    public Task<Table?> GetTableByIdAsync(string id) => T.R(db.Tables.GetValueOrDefault(id));
    public Task<IEnumerable<Table>> GetAllTablesAsync() => T.R<IEnumerable<Table>>(db.Tables.Values.ToList());

    public Task<IEnumerable<Table>> GetTablesByLocationIdAsync(string locationId) =>
        T.R<IEnumerable<Table>>(db.Tables.Values.Where(t => t.LocationId == locationId).ToList());

    public Task SaveTableAsync(Table t) { db.Tables[t.Id] = t; return T.Done; }
    public Task DeleteTableAsync(Table t) { db.Tables.TryRemove(t.Id, out _); return T.Done; }
}

public sealed class InMemoryWaiterRepository(LocalDb db) : IWaiterRepository
{
    public Task<WaiterListItem?> GetWaiterByIdAsync(string userId) => T.R(db.Waiters.GetValueOrDefault(userId));

    public Task<List<WaiterListItem>> GetWaitersByLocationIdAsync(string locationId) =>
        T.R(db.Waiters.Values.Where(w => w.LocationId == locationId).ToList());

    public Task<List<WaiterListItem>> GetAllWaitersAsync() => T.R(db.Waiters.Values.ToList());
    public Task DeleteWaiterAsync(WaiterListItem w) { db.Waiters.TryRemove(w.UserId, out _); return T.Done; }
    public Task SaveWaiterAsync(WaiterListItem w) { db.Waiters[w.UserId] = w; return T.Done; }
}
