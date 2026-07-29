using Market.API.Extensions;
using Market.API.Filters;
using Market.API.Logging;
using Market.API.Middleware;
using Market.API.Persistence;
using Market.API.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var logsPath = Path.Combine(builder.Environment.ContentRootPath, "Logs");
builder.Logging.ClearProviders();
builder.Logging.AddProvider(new RollingFileLoggerProvider(logsPath));

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiResponseWrapperFilter>();
});
builder.Services.AddMarketApiServices(builder.Configuration);
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

//// Seed roles
//using (var scope = app.Services.CreateScope())
//{
//    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
//    await SeedRolesAsync(roleManager);
//}
// Seed roles (after migrations are applied)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    await SeedRolesAsync(roleManager);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Seeds the required roles if they don't already exist
/// </summary>
static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
{
    var roles = new[] { "Customer", "StoreOwner" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new ApplicationRole { Name = role });
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}
