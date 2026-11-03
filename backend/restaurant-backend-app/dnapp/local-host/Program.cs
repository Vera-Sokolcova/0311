using System.Text;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler;
using APIHandler.Local;

// Runs the Lambda API as a regular web server with in-memory data (no AWS).
Environment.SetEnvironmentVariable("LOCAL_MODE", "true");
LocalMode.ApplyDefaults();

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();
var function = new global::APIHandler.Function();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Map("/{**path}", async (HttpContext http) =>
{
    var req = http.Request;
    string body;
    using (var reader = new StreamReader(req.Body, Encoding.UTF8))
        body = await reader.ReadToEndAsync();

    var headers = req.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    var query = req.Query.ToDictionary(q => q.Key, q => q.Value.ToString());

    // Replaces the API Gateway Cognito authorizer: valid Bearer token -> claims.
    Dictionary<string, string>? claims = null;
    var auth = req.Headers.Authorization.ToString();
    if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        claims = await LocalServices.Auth.ValidateAsync(auth["Bearer ".Length..].Trim());

    var lambdaRequest = new APIGatewayProxyRequest
    {
        HttpMethod = req.Method,
        Path = req.Path.Value ?? "/",
        Headers = headers,
        QueryStringParameters = query.Count > 0 ? query : null,
        Body = string.IsNullOrEmpty(body) ? null : body,
        RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
        {
            Authorizer = new APIGatewayCustomAuthorizerContext
            {
                Claims = claims ?? new Dictionary<string, string>()
            }
        }
    };

    var response = await function.FunctionHandler(lambdaRequest, new LocalLambdaContext());

    http.Response.StatusCode = response.StatusCode;
    foreach (var h in response.Headers ?? new Dictionary<string, string>())
        http.Response.Headers[h.Key] = h.Value;
    http.Response.Headers["Access-Control-Allow-Origin"] = "*";
    http.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
    http.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";

    if (!string.IsNullOrEmpty(response.Body))
    {
        if (response.IsBase64Encoded)
            await http.Response.Body.WriteAsync(Convert.FromBase64String(response.Body));
        else
            await http.Response.WriteAsync(response.Body);
    }
});

app.Run();

sealed class LocalLambdaContext : ILambdaContext
{
    public string AwsRequestId { get; } = Guid.NewGuid().ToString();
    public IClientContext ClientContext => null!;
    public string FunctionName => "local-api";
    public string FunctionVersion => "1";
    public ICognitoIdentity Identity => null!;
    public string InvokedFunctionArn => "local";
    public ILambdaLogger Logger { get; } = new ConsoleLambdaLogger();
    public string LogGroupName => "local";
    public string LogStreamName => "local";
    public int MemoryLimitInMB => 512;
    public TimeSpan RemainingTime => TimeSpan.FromMinutes(1);
}

sealed class ConsoleLambdaLogger : ILambdaLogger
{
    public void Log(string message) => Console.Write(message);
    public void LogLine(string message) => Console.WriteLine(message);
}
