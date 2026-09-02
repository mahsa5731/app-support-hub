using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;

namespace AppSupportHub.Web.Security;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddPortfolioSecurity(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpContextAccessor();
        services.AddSingleton(serviceProvider => PortfolioAccounts.FromConfiguration(
            serviceProvider.GetRequiredService<IConfiguration>()));
        services.AddScoped<CurrentActor>();
        services.AddRazorPages(options =>
        {
            options.Conventions.AuthorizePage(
                "/Systems/Create",
                SecurityPolicies.AdministratorOnly);
            options.Conventions.AuthorizePage(
                "/Systems/Edit",
                SecurityPolicies.AdministratorOnly);
            options.Conventions.AuthorizePage(
                "/WorkItems/Create",
                SecurityPolicies.AnalystOrAdministrator);
            options.Conventions.AuthorizePage(
                "/WorkItems/Edit",
                SecurityPolicies.AnalystOrAdministrator);
        });
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "AppSupportHub.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = false;
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Events.OnRedirectToLogin = context =>
                    RedirectPageOrSetApiStatus(context, StatusCodes.Status401Unauthorized);
                options.Events.OnRedirectToAccessDenied = context =>
                    RedirectPageOrSetApiStatus(context, StatusCodes.Status403Forbidden);
            });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                SecurityPolicies.AnalystOrAdministrator,
                policy => policy.RequireRole(
                    SecurityRoles.Analyst,
                    SecurityRoles.Administrator));
            options.AddPolicy(
                SecurityPolicies.AdministratorOnly,
                policy => policy.RequireRole(SecurityRoles.Administrator));
        });
        services.AddAntiforgery(options => options.HeaderName = SecurityPolicies.AntiforgeryHeader);
        services.AddRateLimiter(ConfigureRateLimiting);
        return services;
    }

    public static IApplicationBuilder UsePortfolioSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.Use(async (context, next) =>
        {
            IHeaderDictionary headers = context.Response.Headers;
            headers["Content-Security-Policy"] =
                "default-src 'self'; script-src 'self'; style-src 'self'; "
                + "img-src 'self' data:; form-action 'self'; object-src 'none'; "
                + "frame-ancestors 'none'; base-uri 'self'";
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            await next(context);
        });
    }

    public static RouteGroupBuilder RequireApiAntiforgery(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        group.AddEndpointFilter(async (context, next) =>
        {
            IAntiforgeryValidationFeature? validation = context.HttpContext.Features
                .Get<IAntiforgeryValidationFeature>();
            return validation is null || validation.IsValid
                ? await next(context)
                : Results.BadRequest();
        });
        return group;
    }

    private static void ConfigureRateLimiting(RateLimiterOptions options)
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            IsLoginAttempt(context)
                ? RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => Window(5))
                : RateLimitPartition.GetNoLimiter("not-login"));
        options.AddPolicy(SecurityPolicies.UnsafeApiRateLimit, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.User.Identity?.Name
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown",
                _ => Window(30)));
        options.OnRejected = (context, _) =>
        {
            if (IsLoginAttempt(context.HttpContext))
            {
                ILogger logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AppSupportHub.Security");
                SecurityLog.LoginRateLimited(logger);
            }

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return ValueTask.CompletedTask;
        };
    }

    private static FixedWindowRateLimiterOptions Window(int permitLimit) => new()
    {
        PermitLimit = permitLimit,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        AutoReplenishment = true,
    };

    private static bool IsLoginAttempt(HttpContext context) =>
        HttpMethods.IsPost(context.Request.Method)
        && string.Equals(context.Request.Path.Value, "/Account/Login", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(
            context.Request.Query["handler"].ToString(),
            "Logout",
            StringComparison.OrdinalIgnoreCase);

    private static Task RedirectPageOrSetApiStatus(
        RedirectContext<CookieAuthenticationOptions> context,
        int statusCode)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = statusCode;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }

        return Task.CompletedTask;
    }
}
