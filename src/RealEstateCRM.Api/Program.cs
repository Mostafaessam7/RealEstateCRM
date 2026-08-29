using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Leads.Validators;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Infrastructure;
using RealEstateCRM.Infrastructure.Auth;
using RealEstateCRM.Api.Configuration;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Jobs;
using RealEstateCRM.Infrastructure.Realtime;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Optional Azure Key Vault integration. Set KeyVault__Uri to pull secrets from a vault instead of
// (or on top of) environment variables. Off by default, so nothing changes for anyone not using
// Azure. It must stay above SecretsValidator: a value supplied by the vault has to count as
// configured, otherwise using Key Vault properly would trip the very check that exists to catch
// unconfigured secrets.
//
// DefaultAzureCredential resolves a managed identity in Azure, or `az login` locally.
//
// Key Vault secret names cannot contain ':', so they use '--' instead: a secret named "Jwt--Key"
// maps onto the Jwt:Key configuration entry.
var keyVaultUri = builder.Configuration["KeyVault:Uri"];

if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new Azure.Identity.DefaultAzureCredential());
}

// Runs before any secret is consumed. Jwt:Key ships empty, which does not fail closed - the API
// starts fine in Production and only hits the problem at the first sign-in. See SecretsValidator.
RealEstateCRM.Api.Configuration.SecretsValidator.EnsureProductionSecretsAreConfigured(
    builder.Configuration,
    builder.Environment);

// Add services to the container.

// Every enum in every DTO was serializing as its raw integer value (System.Text.Json's default)
// — the whole frontend (StatusBadge/statusVariant.ts, and every raw `{entity.status}`/
// `{entity.source}` interpolation) was built assuming string enum names ("New", "Available",
// "Contracted", ...), matching how enum columns are stored in the database
// (`.HasConversion<string>()` throughout Infrastructure/Persistence/Configurations). This never
// surfaced in `dotnet build`/`dotnet test` (DTOs are asserted against directly, never through
// real JSON serialization) or in the frontend's own tests (fake API responses were written with
// string status values by hand) — only caught by actually running the full stack end-to-end.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<CreateLeadRequestValidator>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // Browsers can't set an Authorization header on the WebSocket upgrade request, so
        // SignalR clients send the token as a query string param instead — accept it only
        // for the notifications hub path.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    })
    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationSchemeOptions.SchemeName, null);

// Public API (/api/v1) rate limiting, partitioned by API key (or authenticated user id as a
// fallback for JWT-authenticated mobile clients) — 120 requests/minute, no burst queue.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("PublicApi", httpContext =>
    {
        var partitionKey = httpContext.Request.Headers[ApiKeyAuthenticationSchemeOptions.HeaderName].FirstOrDefault()
            ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });

    // Unauthenticated — partition by IP only. Tighter limit than the API-key surface since
    // there's no revocable credential to fall back on if it's abused.
    options.AddPolicy("Marketplace", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });

    // Login/refresh/forgot-password/reset-password had no rate limiting at all — unlimited
    // credential-stuffing, brute-force, and forgot-password email-bombing were all possible.
    // Partitioned by IP (these are all [AllowAnonymous], so there's no user id to key on yet).
    // Tighter than Marketplace since these are write/state-changing and higher-value targets.
    options.AddPolicy("Auth", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

// Any orchestrator (App Service, Kubernetes, Container Apps) needs an endpoint to probe before
// routing traffic to an instance or recycling it.
builder.Services.AddHealthChecks()
    .AddCheck<RealEstateCRM.Api.HealthChecks.DatabaseHealthCheck>("database", tags: ["ready"]);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Roles.SuperAdmin, p => p.RequireRole(Roles.SuperAdmin))
    .AddPolicy(Roles.CompanyAdmin, p => p.RequireRole(Roles.CompanyAdmin))
    .AddPolicy(Roles.SalesManager, p => p.RequireRole(Roles.SalesManager))
    .AddPolicy(Roles.SalesAgent, p => p.RequireRole(Roles.SalesAgent));

// The React app runs on a different origin (Vite dev server, or a separate Azure Web App in
// production) than the API, so without an explicit CORS policy the browser blocks every
// request. Allow-listed origins only — never AllowAnyOrigin. AllowCredentials is required for
// the web app's optional httpOnly-cookie auth transport (see WebAuthCookies) to work — it's
// only usable together with an explicit origin allow-list (never with AllowAnyOrigin, which
// ASP.NET Core CORS refuses to combine with AllowCredentials).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var statusCode = error is AppException appException ? appException.StatusCode : StatusCodes.Status500InternalServerError;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : error!.Message
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    // 1 year, subdomains included — standard once TLS is confirmed to always work in
    // production (Azure Web Apps terminate TLS for us; see docs/deployment.md).
    app.UseHsts();
}

// No security-headers middleware existed at all before this — every response was missing the
// baseline hardening headers. This is a pure API (no HTML views), so most of a typical CSP is
// moot, but these are cheap and defend the few HTML-serving edge cases (error pages, Swagger).
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<NotificationsHub>("/hubs/notifications");

// /health/live  - is the process up? No dependency checks, so a failure here genuinely does mean
//                 "restart me".
// /health/ready - is this instance fit to serve traffic? Adds the database check, so a failure
//                 means "stop routing to me", not "recycle me".
// Both unauthenticated by necessity: the orchestrator doing the probing has no bearer token.
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });

// Set Hangfire:DashboardUsername/DashboardPassword before deploying anywhere network-reachable
// — HangfireDashboardAuthorizationFilter requires HTTP Basic Auth against them, falling back
// to local-requests-only (safe default for local dev) when unset. See docs/deployment.md.
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[]
    {
        new HangfireDashboardAuthorizationFilter(
            builder.Configuration["Hangfire:DashboardUsername"],
            builder.Configuration["Hangfire:DashboardPassword"])
    }
});

// Deployment tasks (role seeding, recurring job registration) no longer run on every boot.
// They used to, which meant every instance wrote to the database as it started: on a cold deploy
// that is N concurrent writers, and because it ran before the app could serve traffic, a collision
// was a failed start rather than a failed job.
//
// Run them as an explicit deployment step:  dotnet RealEstateCRM.Api.dll --init
// See DeploymentInitializer and docs/deployment.md.
{
    var initLogger = app.Services.GetRequiredService<ILoggerFactory>()
        .CreateLogger("RealEstateCRM.Api.DeploymentInitializer");

    if (DeploymentInitializer.IsRequested(args))
    {
        // Init-and-exit: never starts the listener, so an orchestrator can run this as a job.
        await DeploymentInitializer.RunAsync(app.Services, initLogger);
        return;
    }

    if (DeploymentInitializer.ShouldRunOnStartup(app.Environment))
    {
        // Development only. One local instance cannot race itself, and requiring a second command
        // before the app is usable is friction that gets worked around rather than followed.
        await DeploymentInitializer.RunAsync(app.Services, initLogger);
    }
}

app.Run();
