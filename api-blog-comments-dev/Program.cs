using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using api_blog_comments_dev.Configuration;
using api_blog_comments_dev.Data;
using api_blog_comments_dev.Infrastructure;
using api_blog_comments_dev.OpenApi;
using api_blog_comments_dev.Repositories;
using api_blog_comments_dev.Services;

var builder = WebApplication.CreateBuilder(args);
ValidateJwtConfiguration(builder.Configuration);
var documentationEnabled = builder.Configuration.GetValue<bool?>("Documentation:Enabled") ?? builder.Environment.IsDevelopment();
var documentationRoutePrefix = NormalizeRoutePrefix(builder.Configuration.GetValue<string>("Documentation:RoutePrefix") ?? "docs");
var documentationDocumentName = builder.Configuration.GetValue<string>("Documentation:DocumentName") ?? "v1";
var openApiRoutePattern = $"/{documentationRoutePrefix}/openapi/{{documentName}}.json";
var openApiDocumentPath = $"/{documentationRoutePrefix}/openapi/{documentationDocumentName}.json";
var corsAllowedOrigins = GetCorsAllowedOrigins(builder.Configuration);
var dataProtectionKeysPath = builder.Configuration.GetValue<string>("DataProtection:KeysPath");
var useForwardedHeaders = builder.Configuration.GetValue<bool?>("HttpPipeline:UseForwardedHeaders") ?? false;
var useHttpsRedirection = builder.Configuration.GetValue<bool?>("HttpPipeline:UseHttpsRedirection") ?? true;

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context => EnrichProblemDetails(context.ProblemDetails, context.HttpContext);
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Title = "Validation Error",
            Detail = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest
        };

        EnrichProblemDetails(problemDetails, context.HttpContext);

        return new BadRequestObjectResult(problemDetails);
    };
});
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod |
                            HttpLoggingFields.RequestPath |
                            HttpLoggingFields.ResponseStatusCode |
                            HttpLoggingFields.Duration;
    options.RequestHeaders.Add(CorrelationIdMiddleware.HeaderName);
    options.ResponseHeaders.Add(CorrelationIdMiddleware.HeaderName);
});
builder.Services.AddAuthorization(AuthorizationPolicies.Configure);
var authRateLimitingEnabled = builder.Configuration.GetValue<bool?>("AuthRateLimiting:Enabled") ?? true;
var authRateLimitPermitLimit = builder.Configuration.GetValue<int?>("AuthRateLimiting:PermitLimit") ?? 10;
var authRateLimitWindowSeconds = builder.Configuration.GetValue<int?>("AuthRateLimiting:WindowSeconds") ?? 60;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("AuthEndpoints", httpContext =>
    {
        if (!authRateLimitingEnabled)
        {
            return RateLimitPartition.GetNoLimiter("AuthEndpointsDisabled");
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimitPermitLimit,
                Window = TimeSpan.FromSeconds(authRateLimitWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});
builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer<JwtBearerSecuritySchemeTransformer>();
});
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    builder.Services
        .AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

if (useForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// CORS - ajustar origens permitidas conforme o front-end
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        if (corsAllowedOrigins.Length == 0)
        {
            return;
        }

        policy
            .WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Persistência com provider configurável e SQL explícito via Dapper.
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<IUsersRepository, DapperUsersRepository>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddScoped<IPostsRepository, DapperPostsRepository>();

// Configurações de JWT
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection.GetValue<string>("Issuer");
var jwtAudience = jwtSection.GetValue<string>("Audience");
var jwtKey = jwtSection.GetValue<string>("Key") ?? string.Empty;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IPasswordHasher, Argon2IdPasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();


var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseHttpLogging();

using (var scope = app.Services.CreateScope())
{
    var databaseInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await databaseInitializer.InitializeAsync();
}

if (documentationEnabled)
{
    app.Use(async (context, next) =>
    {
        if (!context.Request.Path.Equals(openApiDocumentPath, StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var originalResponseBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await next();

            responseBuffer.Position = 0;
            var openApiJson = await new StreamReader(responseBuffer).ReadToEndAsync();
            var normalizedOpenApiJson = openApiJson.Replace("\"#/components/securitySchemes/Bearer\"", "\"Bearer\"");
            var responseBytes = Encoding.UTF8.GetBytes(normalizedOpenApiJson);

            context.Response.Body = originalResponseBody;
            context.Response.ContentLength = responseBytes.Length;
            await context.Response.Body.WriteAsync(responseBytes);
        }
        finally
        {
            context.Response.Body = originalResponseBody;
        }
    });

    app.MapOpenApi(openApiRoutePattern);

    app.MapScalarApiReference($"/{documentationRoutePrefix}", (options, _) =>
    {
        options.WithTitle("API Blog Comments")
            .WithOpenApiRoutePattern(openApiRoutePattern)
            .AddPreferredSecuritySchemes(JwtBearerDefaults.AuthenticationScheme);
    });
}

if (useForwardedHeaders)
{
    app.UseForwardedHeaders();
}

if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseCors("DefaultCorsPolicy");

app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("ready")
});

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

static void ValidateJwtConfiguration(IConfiguration configuration)
{
    const string placeholderJwtKey = "CHANGE_THIS_SECRET_KEY_IN_PRODUCTION_32_BYTES_MINIMUM";

    var jwtSection = configuration.GetSection("Jwt");
    var jwtKey = jwtSection.GetValue<string>("Key") ?? string.Empty;

    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey == placeholderJwtKey || jwtKey.Length < 32)
    {
        throw new InvalidOperationException(
            "Jwt:Key must be configured with a non-placeholder secret of at least 32 characters. " +
            "Use environment variables, secrets management, or the hosting platform configuration.");
    }
}

static string NormalizeRoutePrefix(string routePrefix)
{
    var normalizedRoutePrefix = routePrefix.Trim('/');

    if (string.IsNullOrWhiteSpace(normalizedRoutePrefix))
    {
        throw new InvalidOperationException("Documentation:RoutePrefix must be configured with a non-empty path.");
    }

    return normalizedRoutePrefix;
}

static string[] GetCorsAllowedOrigins(IConfiguration configuration)
{
    var configuredOrigins = configuration
        .GetSection("Cors:AllowedOrigins")
        .GetChildren()
        .Select(section => section.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Cast<string>();

    var csvOrigins = (configuration.GetValue<string>("Cors:AllowedOriginsCsv") ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    return configuredOrigins
        .Concat(csvOrigins)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

static void EnrichProblemDetails(ProblemDetails problemDetails, HttpContext httpContext)
{
    problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

    if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var correlationId) && correlationId is string correlationIdValue)
    {
        problemDetails.Extensions["correlationId"] = correlationIdValue;
    }
}

public partial class Program;
