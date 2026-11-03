using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using APIHandler.Constants;
using APIHandler.Constants.Dishes;
using APIHandler.Entities;
using APIHandler.Models.Feedbacks;
using APIHandler.Models.WaiterList;

namespace APIHandler.Local;

/// <summary>Process-wide in-memory storage that replaces DynamoDB. Data is lost on restart, then re-seeded.</summary>
public sealed class LocalDb
{
    public const string OwnerSub = "11111111-1111-1111-1111-111111111111";
    public const string WaiterSub = "22222222-2222-2222-2222-222222222222";
    public const string LocationId = "loc-1";
    public const string TableId = "table-1";
    public const string ReservationId = "demo-reservation";

    public ConcurrentDictionary<string, Dish> Dishes { get; } = new();
    public ConcurrentDictionary<string, Location> Locations { get; } = new();
    public ConcurrentDictionary<string, Table> Tables { get; } = new();
    public ConcurrentDictionary<string, Reservation> Reservations { get; } = new();
    public ConcurrentDictionary<string, Order> Orders { get; } = new();
    public ConcurrentDictionary<string, GroupReservation> GroupReservations { get; } = new();
    public ConcurrentDictionary<string, GuestSession> GuestSessions { get; } = new();
    public ConcurrentDictionary<string, Notification> Notifications { get; } = new();
    public ConcurrentDictionary<string, Feedback> Feedbacks { get; } = new();
    public ConcurrentDictionary<string, WaiterListItem> Waiters { get; } = new();

    public LocalDb() => Seed();

    private void Seed()
    {
        Locations[LocationId] = new Location
        {
            Id = LocationId,
            Address = "12 Demo Street, Kyiv",
            Description = "Demo restaurant (local mode)",
            TotalCapacity = 40,
            AverageOccupancy = "50",
            Rating = 4.5,
            SpecialtyDishIds = ["dish-1"]
        };

        Tables[TableId] = new Table { Id = TableId, LocationId = LocationId, Capacity = 6, TableNumber = 1 };
        Tables["table-2"] = new Table { Id = "table-2", LocationId = LocationId, Capacity = 4, TableNumber = 2 };

        AddDish("dish-1", "Bruschetta", DishType.Appetizer, 6.5m);
        AddDish("dish-2", "Pasta Carbonara", DishType.MainCourse, 12m);
        AddDish("dish-3", "Tiramisu", DishType.Dessert, 5m);

        Waiters[WaiterSub] = new WaiterListItem { UserId = WaiterSub, LocationId = LocationId, CreatedAt = DateTime.UtcNow };

        var start = DateTime.UtcNow.Date.AddDays(1).AddHours(17);
        Reservations[ReservationId] = new Reservation
        {
            Id = ReservationId,
            TableId = TableId,
            CustomerId = OwnerSub,
            WaiterId = WaiterSub,
            StartTime = start,
            EndTime = start.AddHours(2),
            GuestCount = 4,
            Status = ReservationStatus.Reserved
        };

        Orders["order-demo"] = new Order
        {
            OrderId = "order-demo",
            CustomerId = OwnerSub,
            ReservationId = ReservationId,
            ReservationAddress = "12 Demo Street, Kyiv",
            ReservationTime = start.ToString("HH:mm") + " - " + start.AddHours(2).ToString("HH:mm"),
            Dishes = [],
            Status = OrderStatus.Created
        };
    }

    private void AddDish(string id, string name, DishType type, decimal price) =>
        Dishes[id] = new Dish
        {
            Id = id,
            Name = name,
            Description = name + " (demo)",
            DishType = type,
            Price = price,
            Weight = "250 g",
            Calories = "400",
            Proteins = "12",
            Fats = "15",
            Carbohydrates = "40",
            Vitamins = "B, C",
            State = DishState.Available,
            LocationId = LocationId
        };
}
