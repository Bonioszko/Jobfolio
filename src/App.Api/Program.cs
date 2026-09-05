using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using App.Api;
using App.Api.Authentication;
using App.Api.Endpoints;
using App.Application;
using App.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLocalInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentWorkspaceAccessor, CurrentWorkspaceAccessor>();
var googleAuthentication = GoogleAuthenticationSettings.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(googleAuthentication);
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var authentication = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "jobparser.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnValidatePrincipal = SessionCookieValidator.ValidateAsync;
    });
authentication.AddGoogleOpenIdConnect(googleAuthentication);
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => options.AddPolicy("demo-session", context =>
    RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ =>
        new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0
        })));
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider
        .GetRequiredService<IApplicationDatabaseInitializer>()
        .InitializeAsync(CancellationToken.None);
app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapApplicationEndpoints();
app.Run();
