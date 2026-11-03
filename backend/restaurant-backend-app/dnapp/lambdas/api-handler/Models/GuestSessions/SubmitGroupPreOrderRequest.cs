using System.Text.Json.Serialization;

namespace APIHandler.Models.GuestSessions;

public class SubmitGroupPreOrderRequest
{
    [JsonPropertyName("reservationId")]
    public string ReservationId { get; set; } = string.Empty;

    /// <summary>Optional note for the restaurant, at most 300 characters.</summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}
