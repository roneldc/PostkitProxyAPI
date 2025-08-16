var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;
var apiBase = configuration["ApiSettings:BaseUrl"];
var adminKey = configuration["Tenancy:GlobalAdminApiKey"];
var uiOrigin = configuration["UiSettings:Origin"];

// Services
builder.Services
    .AddCors(options =>
    {
        options.AddPolicy("AllowUI", policy =>
        {
            policy.WithOrigins(uiOrigin!)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    })
    .AddHttpClient("PostkitApi", client =>
    {
        client.BaseAddress = new Uri(apiBase!);
    });

var app = builder.Build();

// Middleware
app.UseCors("AllowUI");

// Endpoints
app.MapPost("/ui/tenants", async (CreateTenantDto dto, IHttpClientFactory httpFactory, ILogger<Program> logger) =>
{
    logger.LogInformation("Received request to create tenant: {@Dto}", dto);

    if (string.IsNullOrWhiteSpace(dto.AppName) || string.IsNullOrWhiteSpace(dto.Email))
    {
        return Results.BadRequest("AppName and Email are required.");
    }

    var client = httpFactory.CreateClient("PostkitApi");

    logger.LogInformation("Creating HTTP request to Postkit API with AppName: {AppName}, Email: {Email}", dto.AppName, dto.Email);
    var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/tenants")
    {
        Content = JsonContent.Create(dto)
    };

    request.Headers.Add("X-Admin-ApiKey", adminKey);

    var response = await client.SendAsync(request);
    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        return Results.Json(new { message = error?.Message }, statusCode: (int)response.StatusCode);
    }

    logger.LogInformation("Tenant created successfully, reading response content.");

    var created = await response.Content.ReadFromJsonAsync<ApiResponse>();
    if (created is null)
    {
        return Results.Json(new { error = "Invalid response from API." }, statusCode: 500);
    }

    logger.LogInformation("Tenant created with ID: {TenantId}", created.Data?.TenantId);
    return Results.Ok(created);
})
.RequireCors("AllowUI");

app.MapGet("/ui/tenants/{tenantId}/credentials", async (string tenantId, string token, IHttpClientFactory httpFactory, ILogger<Program> logger) =>
{
    logger.LogInformation("Received request for tenant credentials: TenantId={TenantId}, Token={Token}", tenantId, token);
    if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(token))
    {
        return Results.BadRequest("TenantId and Token is required.");
    }

    var client = httpFactory.CreateClient("PostkitApi");
    var url = $"api/v1/tenants/{tenantId}/credentials?token={token}";

    logger.LogInformation("Creating HTTP request to Postkit API for credentials: {Url}", url);
    var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Add("X-Tenant-Id", tenantId);

    var resp = await client.SendAsync(request);

    if (!resp.IsSuccessStatusCode)
    {
        var error = await resp.Content.ReadAsStringAsync();
        return Results.Json(new { error }, statusCode: (int)resp.StatusCode);
    }

    logger.LogInformation("Credentials retrieved successfully for TenantId: {TenantId}", tenantId);

    var credentials = await resp.Content.ReadFromJsonAsync<ApiResponse>();
    if (credentials is null)
    {
        return Results.Json(new { error = "Invalid response from API." }, statusCode: 500);
    }

    logger.LogInformation("{tenant} credentials retrieved successfully.", tenantId);
    return Results.Ok(credentials.Data);
})
.RequireCors("AllowUI");

app.MapGet("/", () => Results.Ok("Postkit Proxy API is running"));
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

public record CreateTenantDto(string AppName, string Email);
public record ApiResponse(
    bool Success,
    int Status,
    string Message,
    TenantData? Data,
    DateTime Timestamp
);

public record ErrorResponse(
        int StatusCode,
        string Message,
        DateTime Timestamp
    );

public record TenantData(
    string TenantId,
    string ApiKey,
    string TenantEmail
);