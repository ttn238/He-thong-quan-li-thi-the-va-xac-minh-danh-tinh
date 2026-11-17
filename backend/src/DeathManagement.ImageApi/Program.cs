using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var jwtKey = builder.Configuration["Jwt:Key"];
if (Encoding.UTF8.GetByteCount(jwtKey ?? "") < 32)
    throw new InvalidOperationException("Jwt:Key must be configured with at least 32 bytes and match the desktop application's signing key.");

builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, DesktopJwtHandler>("Bearer", _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AiAnalyze", p => p.RequireAuthenticatedUser().RequireAssertion(c =>
        HasPermission(c.User, "VIEW") || HasPermission(c.User, "CONFIRM")));
    options.AddPolicy("AiView", p => p.RequireAuthenticatedUser().RequireAssertion(c =>
        HasPermission(c.User, "VIEW") || HasPermission(c.User, "CONFIRM")));
    options.AddPolicy("AiReview", p => p.RequireAuthenticatedUser().RequireAssertion(c =>
        HasPermission(c.User, "CONFIRM")));
});
builder.Services.AddControllers();

var aiUrl = builder.Configuration["AiService:BaseUrl"] ?? "http://localhost:8000";
var timeoutSeconds = int.TryParse(builder.Configuration["AiService:TimeoutSeconds"], out var seconds) && seconds > 0 ? seconds : 45;
builder.Services.AddHttpClient<AiServiceClient>(client =>
{
    client.BaseAddress = new Uri(aiUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "image-api" }));
app.MapControllers();
app.Run();

static bool HasPermission(ClaimsPrincipal user, string action) => user.FindAll("perm")
    .Any(c => c.Value.Equals($"AI_RESULT:{action}:ALL", StringComparison.OrdinalIgnoreCase)
        || c.Value.Equals($"AI_RESULT:{action}:PARTIAL", StringComparison.OrdinalIgnoreCase)
        || c.Value.Equals($"AI_RESULT:{action}:OWN", StringComparison.OrdinalIgnoreCase));

public sealed class DesktopJwtHandler(
    Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());
        try
        {
            var parts = header[7..].Split('.');
            if (parts.Length != 3) return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
            using var head = JsonDocument.Parse(Decode(parts[0]));
            if (head.RootElement.GetProperty("alg").GetString() != "HS256")
                return Task.FromResult(AuthenticateResult.Fail("Unsupported token algorithm"));
            var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);
            var expected = HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]));
            if (!CryptographicOperations.FixedTimeEquals(expected, Decode(parts[2])))
                return Task.FromResult(AuthenticateResult.Fail("Invalid token signature"));
            using var payload = JsonDocument.Parse(Decode(parts[1]));
            var root = payload.RootElement;
            var issuer = configuration["Jwt:Issuer"] ?? "DeathManagement.API";
            var audience = configuration["Jwt:Audience"] ?? "DeathManagement.Client";
            if (root.GetProperty("iss").GetString() != issuer || !HasAudience(root, audience))
                return Task.FromResult(AuthenticateResult.Fail("Invalid token issuer or audience"));
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (!root.TryGetProperty("exp", out var exp) || exp.GetInt64() <= now
                || (root.TryGetProperty("nbf", out var nbf) && nbf.GetInt64() > now + 30))
                return Task.FromResult(AuthenticateResult.Fail("Expired token"));

            var claims = new List<Claim>();
            AddStringClaim(root, claims, "sub");
            AddStringClaim(root, claims, "name");
            AddStringClaim(root, claims, "username");
            AddStringClaim(root, claims, "role");
            AddStringClaim(root, claims, "unit");
            if (root.TryGetProperty("perm", out var permissions))
            {
                if (permissions.ValueKind == JsonValueKind.Array)
                    claims.AddRange(permissions.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => new Claim("perm", x.GetString()!)));
                else if (permissions.ValueKind == JsonValueKind.String)
                    claims.Add(new Claim("perm", permissions.GetString()!));
            }
            if (claims.All(c => c.Type != "sub")) return Task.FromResult(AuthenticateResult.Fail("Missing subject"));
            var identity = new ClaimsIdentity(claims, Scheme.Name, "name", "role");
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
        catch (Exception ex) when (ex is FormatException or JsonException or KeyNotFoundException or InvalidOperationException or ArgumentException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
        }
    }

    private static byte[] Decode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
        return Convert.FromBase64String(normalized);
    }

    private static bool HasAudience(JsonElement root, string audience)
    {
        if (!root.TryGetProperty("aud", out var value)) return false;
        return value.ValueKind == JsonValueKind.String ? value.GetString() == audience
            : value.ValueKind == JsonValueKind.Array && value.EnumerateArray().Any(x => x.GetString() == audience);
    }

    private static void AddStringClaim(JsonElement root, List<Claim> claims, string name)
    {
        if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            claims.Add(new Claim(name, value.GetString()!));
    }
}
