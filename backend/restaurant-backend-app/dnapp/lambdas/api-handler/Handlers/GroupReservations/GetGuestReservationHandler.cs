using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Services.GroupReservations;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GuestReservations;

public class GetGroupReservationHandler(IGroupReservationService groupReservationService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.TryGetPathValue("token", out string? token))
            return ResponseBuilder.CreateErrorResponse(400, "Token is requried.", context.Logger);

        try
        {
            string? userId = request.RequestContext is null ? null : UserContextHelper.GetAuthenticatedUserId(request);
            var result = userId is null
                ? await groupReservationService.GetGroupReservationByTokenAsync(token)
                : await groupReservationService.GetGroupReservationForOwnerAsync(token, userId);
            return ResponseBuilder.CreateSuccessResponse(result);
        }
        catch(GroupReservationNotFoundException exception)
        {
            return ResponseBuilder.CreateErrorResponse(404, exception.Message, context.Logger, exception);
        }
        catch (ValidationException exception)
        {
            return ResponseBuilder.CreateErrorCodeResponse(exception);
        }
    }
}
