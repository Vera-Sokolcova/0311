using System;

namespace APIHandler.Local;

/// <summary>
/// LOCAL_MODE=true runs the API without any AWS service (in-memory data, built-in auth).
/// Used for free hosting (Render, etc.). In Lambda the flag is not set, so nothing changes.
/// </summary>
public static class LocalMode
{
    public static bool Enabled =>
        string.Equals(Environment.GetEnvironmentVariable("LOCAL_MODE"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>Fills the env vars that ResourceNames requires, so nothing throws in local mode.</summary>
    public static void ApplyDefaults()
    {
        foreach (var name in new[]
                 {
                     "REGION", "COGNITO_USER_POOL_ID", "COGNITO_USER_POOL_CLIENT_ID", "FEEDBACK_TABLE_NAME",
                     "OrderIndexReservationId", "WaiterListTableName", "dishesTable", "feedbackGlobalIndex",
                     "groupReservationsTable", "guestSessionTable", "imagesBucketName", "locationsTable",
                     "notificationsTable", "notificationsUserIdIndex", "ordersTable", "profileImagesBucketName",
                     "profileImagesTable", "reportsHistoryTable", "reportsTable", "reports_queue_name",
                     "reservationsCustomerIdGlobalIndex", "reservationsTable", "reservationsTableGlobalIndex",
                     "reservationsWaiterIdGlobalIndex", "tableLocationIdIndex", "tablesTable", "waitersGlobalIndex"
                 })
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
                Environment.SetEnvironmentVariable(name, name == "REGION" ? "us-east-1" : "local-" + name);
        }
    }
}
