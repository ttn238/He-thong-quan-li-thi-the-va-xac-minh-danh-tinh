using DeathManagement.API.Data;
using DeathManagement.API.Repositories;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Security;
using DeathManagement.API.Services;
using DeathManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DeathManagement.API;

internal static class BodyApiHost
{
    public static void Run(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ApplicationDbContext>(_ => Program.NewDb());
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IRoleRepository, RoleRepository>();
        builder.Services.AddScoped<IRoleService, RoleService>();
        builder.Services.AddScoped<IBodyRepository, BodyRepository>();
        builder.Services.AddScoped<IBodyService, BodyService>();
        builder.Services.AddScoped<IMissingPersonRepository, MissingPersonRepository>();
        builder.Services.AddScoped<IMissingPersonService, MissingPersonService>();
        builder.Services.AddScoped<IStorageRepository, StorageRepository>();
        builder.Services.AddScoped<IStorageService, StorageService>();
        builder.Services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull);
        var app = builder.Build();
        var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
        AccessControl.PrincipalProvider = () => accessor.HttpContext?.User.Identity?.IsAuthenticated == true
            ? accessor.HttpContext.User : null;
        app.Use(async (context, next) =>
        {
            var header = context.Request.Headers.Authorization.ToString();
            context.User = JwtTokenService.Validate(header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? header[7..] : null) ?? new System.Security.Claims.ClaimsPrincipal();
            try { await next(context); }
            catch (Exception ex) when (ex is UnauthenticatedException or ForbiddenException or KeyNotFoundException or ArgumentException or InvalidOperationException)
            {
                context.Response.StatusCode = ex switch
                {
                    UnauthenticatedException => 401,
                    ForbiddenException => 403,
                    KeyNotFoundException => 404,
                    _ => 400
                };
                await context.Response.WriteAsJsonAsync(new { message = ex.Message });
            }
        });
        app.MapControllers();
        app.Run();
    }
}
