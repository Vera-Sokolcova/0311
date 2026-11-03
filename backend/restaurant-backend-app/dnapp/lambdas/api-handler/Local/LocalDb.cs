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
    public const string Waiter2Sub = "33333333-3333-3333-3333-333333333333";
    public const string Waiter3Sub = "44444444-4444-4444-4444-444444444444";
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
    public ConcurrentDictionary<string, ProfileImage> ProfileImages { get; } = new();

    public LocalDb() => Seed();

    private void Seed()
    {
        // Data below comes from the Figma design (Most Popular Dishes, dish modal, Locations).
        AddDish("dish-1", "Fresh Strawberry Mint Salad", DishType.Appetizer, 17m, "fresh-strawberry-mint-salad",
            "Strawberries, spinach, mint leaves, feta or goat cheese, walnuts, balsamic glaze, poppy seeds.",
            "430 g", "503", "18-20", "33-35", "30-35", "Vit C, Vit A, Calcium, and Ferum");
        AddDish("dish-2", "Avocado Pine Nut Bowl", DishType.Appetizer, 17m, "avocado-pine-nut-bowl",
            "Avocado, pine nuts, fresh greens and seasonal vegetables.",
            "430 g", "480", "14-16", "30-34", "25-30", "Vit E, Vit K, Potassium");
        AddDish("dish-3", "Roasted Sweet Potato & Lentil Salad", DishType.Appetizer, 17m, "roasted-sweet-potato-and-lentil-salad",
            "Roasted sweet potato with lentils, herbs and a light dressing.",
            "430 g", "460", "16-18", "18-22", "55-60", "Vit A, Vit C, Iron");
        AddDish("dish-4", "Spring Salad", DishType.Appetizer, 17m, "spring-salad",
            "Fresh spring greens, herbs and seasonal vegetables.",
            "430 g", "320", "8-10", "20-24", "22-26", "Vit C, Vit K, Folate");
        // The remaining dishes are demo extras (not in the design) so that every menu tab has content.
        AddDish("dish-5", "Green Asparagus Salad", DishType.Appetizer, 17m, "green-asparagus-salad",
            "Green asparagus with lemon dressing.", "430 g", "290", "9-11", "16-20", "18-22", "Vit K, Folate",
            DishState.OutOfStock);
        AddDish("dish-6", "Vegetable Khachapuri", DishType.MainCourse, 14m, "vegetable-khachapuri",
            "Georgian cheese bread with seasonal vegetables.", "380 g", "620", "22-25", "28-32", "60-65", "Calcium, Vit A");
        AddDish("dish-7", "Pasta Carbonara", DishType.MainCourse, 15m, "pasta-carbonara",
            "Creamy pasta with herbs.", "350 g", "640", "20-24", "30-34", "62-68", "Vit B12, Calcium");
        AddDish("dish-8", "Tiramisu", DishType.Dessert, 8m, "tiramisu",
            "Classic coffee dessert.", "180 g", "410", "7-9", "22-26", "40-44", "Calcium");
        AddDish("dish-9", "Honey Walnut Cake", DishType.Dessert, 8m, "honey-walnut-cake",
            "Honey cake with walnuts.", "170 g", "430", "6-8", "24-28", "44-48", "Magnesium");

        var popular = new List<string> { "dish-1", "dish-2", "dish-3", "dish-4", "dish-5" };
        AddLocation(LocationId, "48 Rustaveli Avenue",
            "Located on bustling Rustaveli Avenue, this branch offers a perfect mix of city energy and a cozy atmosphere. With its spacious outdoor terrace, this location is ideal for both casual lunches and intimate dinners.",
            40, "90", "rustaveli-avenue-48", 4.73, popular);
        AddLocation("loc-2", "14 Baratashvili Street",
            "A lively spot with an open kitchen and a summer terrace in the heart of Tbilisi.",
            64, "75", "baratashvili-street-14", 4.5, ["dish-1", "dish-2", "dish-4", "dish-6", "dish-8"]);
        AddLocation("loc-3", "9 Abashidze Street",
            "A quiet place for family dinners with a warm, plant-filled interior.",
            80, "80", "abashidze-street-9", 4.3, ["dish-2", "dish-3", "dish-5", "dish-7", "dish-9"]);

        AddTables(LocationId, "table-", 10);
        AddTables("loc-2", "loc2-table-", 16);
        AddTables("loc-3", "loc3-table-", 20);

        Waiters[WaiterSub] = new WaiterListItem { UserId = WaiterSub, LocationId = LocationId, CreatedAt = DateTime.UtcNow };
        Waiters[Waiter2Sub] = new WaiterListItem { UserId = Waiter2Sub, LocationId = "loc-2", CreatedAt = DateTime.UtcNow };
        Waiters[Waiter3Sub] = new WaiterListItem { UserId = Waiter3Sub, LocationId = "loc-3", CreatedAt = DateTime.UtcNow };

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
            ReservationAddress = "48 Rustaveli Avenue",
            ReservationTime = start.ToString("HH:mm") + " - " + start.AddHours(2).ToString("HH:mm"),
            Dishes = [],
            Status = OrderStatus.Created
        };
    }

    private void AddTables(string locationId, string idPrefix, int count)
    {
        var capacities = new[] { 2, 4, 6, 4 };
        for (var i = 1; i <= count; i++)
            Tables[idPrefix + i] = new Table
            {
                Id = idPrefix + i,
                LocationId = locationId,
                Capacity = capacities[(i - 1) % capacities.Length],
                TableNumber = i
            };
    }

    private void AddLocation(string id, string address, string description, int capacity, string occupancy,
        string image, double rating, List<string> specialtyDishIds) =>
        Locations[id] = new Location
        {
            Id = id,
            Address = address,
            Description = description,
            TotalCapacity = capacity,
            AverageOccupancy = occupancy,
            ImageUrl = $"assets/img/locations/{image}.jpg",
            Rating = rating,
            SpecialtyDishIds = specialtyDishIds
        };

    private void AddDish(string id, string name, DishType type, decimal price, string image, string description,
        string weight, string calories, string proteins, string fats, string carbs, string vitamins,
        DishState state = DishState.Available) =>
        Dishes[id] = new Dish
        {
            Id = id,
            Name = name,
            Description = description,
            DishType = type,
            Price = price,
            Weight = weight,
            Calories = calories,
            Proteins = proteins,
            Fats = fats,
            Carbohydrates = carbs,
            Vitamins = vitamins,
            ImageUrl = $"assets/img/dishes/{image}.jpg",
            State = state,
            LocationId = LocationId
        };
}
