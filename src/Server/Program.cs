using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Areas.Identity;
using Server.Data;
using Server.Services;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
                      ?? "Server=sql;Database=CaptureDb;User Id=sa;Password=Your_strong!Passw0rd;TrustServerCertificate=True";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = ctx =>
        {
            if (IsApiRequest(ctx.HttpContext))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = ctx =>
        {
            if (IsApiRequest(ctx.HttpContext))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();
builder.Services.AddScoped<ProductEntryService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientWasm", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetValue<string>("ClientApp:BaseUrl") ?? "https://localhost:5002")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

await SeedDefaultsAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("ClientWasm");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapGet("/auth/continue", (HttpContext context, [FromQuery] string target) =>
    {
        if (!(context.User.Identity?.IsAuthenticated ?? false))
        {
            return Results.Redirect("/Identity/Account/Login");
        }

        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
        {
            return Results.BadRequest("Invalid target");
        }

        if (!string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Host not allowed");
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Scheme not allowed");
        }

        if (uri.Port is not (5003 or 5002))
        {
            return Results.BadRequest("Port not allowed");
        }

        return Results.Redirect(target);
    })
    .RequireAuthorization();

app.Run();

static async Task SeedDefaultsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = ["DataCollector", "Viewer", "Admin"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var adminEmail = "admin@local";
    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin is null)
    {
        admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            DisplayName = "Administrator"
        };

        var result = await userManager.CreateAsync(admin, "Change_this1!");
        if (result.Succeeded)
        {
            await userManager.AddToRolesAsync(admin, roles);
        }
    }
}

static bool IsApiRequest(HttpContext context)
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        return true;
    }

    if (context.Request.Headers.TryGetValue("Accept", out var acceptHeaders))
    {
        return acceptHeaders.Any(h => h.Contains("application/json", StringComparison.OrdinalIgnoreCase));
    }

    return false;
}
