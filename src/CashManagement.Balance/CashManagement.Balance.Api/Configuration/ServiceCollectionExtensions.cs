using System.Text;
using System.Text.Json;
using CashManagement.Balance.Api.Auth;
using CashManagement.Balance.Api.Contracts;
using CashManagement.Balance.Api.Correlation;
using CashManagement.Balance.Application.Features.GetDailyBalance;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace CashManagement.Balance.Api.Configuration;

/// <summary>Wire-up de DI da API do Balance: Application (handler de consulta), auth e correlação.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra o pipeline da Application: o handler da consulta do saldo. Injetado
    /// direto no controller (sem dispatcher) — a leitura é uma só, então o dispatcher
    /// do Entries (que decora o write com idempotência) não agrega valor aqui (§5.10).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetDailyBalanceQueryHandler>();
        return services;
    }

    /// <summary>Registra o contexto escopado de correlação da borda.</summary>
    public static IServiceCollection AddCorrelation(this IServiceCollection services)
    {
        services.AddScoped<CorrelationContext>();
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
            options.AddPolicy(AuthPolicies.BalancesRead, policy =>
                policy.Requirements.Add(new ScopeRequirement(JwtOptions.ReadScope))));

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
