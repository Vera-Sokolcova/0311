using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.GuestSessions;
using APIHandler.Services.GuestSessions;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GuestSessions;

/// <summary>POST /group-reservations/{groupReservationId}/submit — owner only.</summary>
public class SubmitGroupPreOrderHandler(IGroupPreOrderService service) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.TryGetPathValue("groupReservationId", out string? groupReservationId))
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'groupReservationId' in path parameters.", context.Logger);

        string? userId = UserContextHelper.GetAuthenticatedUserId(request);
        if (userId is null)
            return ResponseBuilder.CreateErrorResponse(401, "Unauthorized.", context.Logger);

        if (!RequestBodyHelper.TryParseAndValidate(request, context, out SubmitGroupPreOrderRequest? body, out var errorResponse))
            return errorResponse;

        if (string.IsNullOrWhiteSpace(body.ReservationId))
            return ResponseBuilder.CreateErrorResponse(400, "'reservationId' is required.", context.Logger);

        try
        {
            var result = await service.SubmitGroupPreOrderAsync(userId, groupReservationId, body.ReservationId);
            return ResponseBuilder.CreateJsonResponse(200, result);
        }
        catch (ActionForbiddenException ex)
        {
            return ResponseBuilder.CreateErrorResponse(403, ex.Message, context.Logger);
        }
        catch (GroupReservationNotFoundException ex)
        {
            return ResponseBuilder.CreateErrorResponse(404, ex.Message, context.Logger);
        }
        catch (ReservationNotFoundException ex)
        {
            return ResponseBuilder.CreateErrorResponse(404, ex.Message, context.Logger);
        }
        catch (GroupReservationSubmittedException ex)
        {
            return ResponseBuilder.CreateErrorResponse(409, ex.Message, context.Logger);
        }
        catch (GroupReservationExpiredException ex)
        {
            return ResponseBuilder.CreateErrorResponse(409, ex.Message, context.Logger);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorCodeResponse(ex);
        }
    }
}
