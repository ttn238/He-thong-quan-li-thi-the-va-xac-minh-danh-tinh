var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Dependency Injection
builder.Services.AddSingleton<DeathManagement.API.Repositories.Interfaces.IUserRepository, DeathManagement.API.Repositories.UserRepository>();
builder.Services.AddScoped<DeathManagement.API.Services.Interfaces.IUserService, DeathManagement.API.Services.UserService>();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Development configuration
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
