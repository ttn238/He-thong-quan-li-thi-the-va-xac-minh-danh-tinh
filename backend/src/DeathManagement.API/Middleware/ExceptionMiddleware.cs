using System.Text.Json;
using DeathManagement.API.Security;
using Microsoft.AspNetCore.Http;

namespace DeathManagement.API.Middleware;

/// <summary>
/// Chuyển exception phân quyền thành mã HTTP đúng chuẩn và không để lộ chi tiết lỗi nội bộ:
/// UnauthenticatedException -> 401, ForbiddenException -> 403, còn lại -> 500 (thông báo chung).
/// Đăng ký trong Program (bản Web API): app.UseMiddleware&lt;ExceptionMiddleware&gt;();
/// đặt TRƯỚC UseAuthentication/UseAuthorization/MapControllers.
/// </summary>
public class ExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (UnauthenticatedException ex)
        {
            await Write(context, ex.StatusCode, ex.Message);
        }
        catch (ForbiddenException ex)
        {
            await Write(context, ex.StatusCode, ex.Message);
        }
        catch (Exception)
        {
            // Không trả stack trace / thông điệp exception cho client
            await Write(context, StatusCodes.Status500InternalServerError, "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.");
        }
    }

    private static Task Write(HttpContext context, int status, string message)
    {
        if (context.Response.HasStarted) return Task.CompletedTask;
        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { message }));
    }
}