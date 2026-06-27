using System.Text;
using System.Text.Json;
using CashManagement.Entries.Api.Auth;
using CashManagement.Entries.Api.Contracts;
using CashManagement.Entries.Api.Correlation;
using CashManagement.Entries.Application.Abstractions;
using CashManagement.Entries.Application.Features.PostCredit;
using CashManagement.Entries.Application.Interfaces;
using CashManagement.Entries.Domain.Persistence;
using CashManagement.Entries.Infrastructure.Messaging;
using CashManagement.Entries.Infrastructure.Persistence;
using CashManagement.Entries.Infrastructure.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CashManagement.Entries.Api.Configuration;

/// <summary>Wire-up de DI da API: Application, Infra, EF Core, auth e correlação.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registra o pipeline da Application (dispatcher + handler + validator).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<PostCreditCommandValidator>();
        services.AddScoped<ICommandHandler<PostCreditCommand, Guid>, PostCreditCommandHandler>();
        return services;
    }

    /// <summary>Registra a Infra: EF Core (SQL Server), repositório, UoW, idempotência, id, publisher e relay.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string kafkaBootstrap)
    {
        services.AddDbContext<EntriesDbContext>(options => options.UseSqlServer(connectionString));

        services.AddSingleton<EventSerializer>();
        services.AddScoped<IRepository, Repository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdGenerator, GuidIdGenerator>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();

        services.AddSingleton<IEventPublisher>(_ => new KafkaEventPublisher(kafkaBootstrap));
        services.AddScoped<OutboxProcessor>();
        services.AddHostedService<OutboxRelayService>();

        return services;
    }

    /// <summary>Registra o contexto de correlação escopado, exposto pela porta da Application.</summary>
    public static IServiceCollection AddCorrelation(this IServiceCollection services)
    {
        services.AddScoped<CorrelationContext>();
        services.AddScoped<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());
        return services;
    }

    /// <summary>Configura JWT Bearer com a chave estática de dev (§9.1) e a política de scope (§8.1).</summary>
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, JwtOptions jwt)
    {
        services.AddSingleton<DevTokenService>();
        services.AddSingleton<IAuthorizationHandler, ScopeHandler>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = WriteUnauthorizedEnvelope,
                    OnForbidden = WriteForbiddenEnvelope,
                };
            });

        services.AddAuthorization(options =>
            options.AddPolicy(AuthPolicies.EntriesWrite, policy =>
                policy.Requirements.Add(new ScopeRequirement(JwtOptions.WriteScope))));

        return services;
    }

    private static async Task WriteUnauthorizedEnvelope(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        await WriteEnvelopeAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
            new ApiError("UNAUTHORIZED", "Authentication is required.", []));
    }

    private static Task WriteForbiddenEnvelope(ForbiddenContext context) =>
        WriteEnvelopeAsync(context.HttpContext, StatusCodes.Status403Forbidden,
            new ApiError("INSUFFICIENT_SCOPE", "The required scope is missing.", []));

    private static async Task WriteEnvelopeAsync(HttpContext httpContext, int statusCode, ApiError error)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var correlationId = httpContext.Response.Headers.TryGetValue(CorrelationMiddleware.HeaderName, out var header)
            && !string.IsNullOrEmpty(header)
                ? header.ToString()
                : httpContext.Request.Headers.TryGetValue(CorrelationMiddleware.HeaderName, out var requestHeader)
                  && !string.IsNullOrEmpty(requestHeader)
                    ? requestHeader.ToString()
                    : Guid.NewGuid().ToString();

        httpContext.Response.Headers[CorrelationMiddleware.HeaderName] = correlationId;
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var body = ApiResponse<object>.Failure(error, correlationId);
        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
