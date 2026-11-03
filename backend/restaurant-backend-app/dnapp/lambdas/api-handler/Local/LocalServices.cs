using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Amazon.SQS;
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
using APIHandler.Services.Reports.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace APIHandler.Local;

public static class LocalServices
{
    public static LocalDb Db { get; } = new();
    public static LocalAuth Auth { get; } = new();

    /// <summary>Replaces section "AWS clients + repositories" of RouteRegistry in LOCAL_MODE.</summary>
    public static void Register(IServiceCollection s)
    {
        s.AddSingleton(Db);
        s.AddSingleton(Auth);

        s.AddSingleton(LocalCognitoProxy.Create(Auth));
        s.AddSingleton(NotSupportedProxy.Create<IAmazonDynamoDB>());
        s.AddSingleton(InMemoryDynamoContextProxy.Create(Db));
        s.AddSingleton(NotSupportedProxy.Create<IAmazonS3>());
        s.AddSingleton(NotSupportedProxy.Create<IAmazonSQS>());

        // Report generation needs S3/SQS/PDF generation; not available in local mode.
        s.AddSingleton(NotSupportedProxy.Create<IReportService>());
        s.AddSingleton(NotSupportedProxy.Create<IReportFileService>());

        s.AddSingleton<IUserRepository, InMemoryUserRepository>();
        s.AddSingleton<IDishRepository, InMemoryDishRepository>();
        s.AddSingleton<ILocationRepository, InMemoryLocationRepository>();
        s.AddSingleton<IWaiterRepository, InMemoryWaiterRepository>();
        s.AddSingleton<INotificationRepository, InMemoryNotificationRepository>();
        s.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        s.AddSingleton<IReservationRepository, InMemoryReservationRepository>();
        s.AddSingleton<IFeedbackRepository, InMemoryFeedbackRepository>();
        s.AddSingleton<IReportRepository, InMemoryReportRepository>();
        s.AddSingleton<IGuestSessionRepository, InMemoryGuestSessionRepository>();
        s.AddSingleton<IGroupReservationRepository, InMemoryGroupReservationRepository>();
        s.AddSingleton<ITableRepository, InMemoryTableRepository>();
    }
}
