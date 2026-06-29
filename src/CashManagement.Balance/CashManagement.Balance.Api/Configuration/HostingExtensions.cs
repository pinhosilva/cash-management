using CashManagement.Balance.Api.Auth;
using CashManagement.Balance.Api.Correlation;
using CashManagement.Balance.Api.Http;
using CashManagement.Balance.Infrastructure;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Formatting.Compact;

namespace CashManagement.Balance.Api.Configuration;

/// <summary>
/// Composition root da API: concentra o wiring num par de pontos navegáveis —
/// <see cref="AddBalanceApi"/> (logging + configuração + registro de serviços) e
/// <see cref="UseBalanceApi"/> (pipeline + endpoints). O <c>Program.cs</c> fica
/// mínimo. Reutiliza a <c>AddBalanceInfrastructure</c> (Mongo + consumer Kafka) da
/// Infrastructure — não duplica a infra do consumer.
/// </summary>
public static class HostingExtensions
{
    private const string DevSigningKey = "dev-signing-key-change-me-please-32-bytes-minimum";

    /// <summary>Registra logging estruturado, configuração e todos os serviços da API.</summary>
    public static WebApplicationBuilder AddBalanceApi(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("service", "CashManagement.Balance")
            .Enrich.WithProperty("environment", context.HostingEnvironment.EnvironmentName)
            .WriteTo.Console(new CompactJsonFormatter()));

        var jwt = ResolveJwt(builder);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.Configure<FeatureFlags>(builder.Configuration.GetSection(FeatureFlags.SectionName));

        builder.Services.AddApplication();
        builder.Services.AddBalanceInfrastructure(builder.Configuration);
        builder.Services.AddCorrelation();
        builder.Services.AddJwtAuth(jwt);
        builder.Services.AddBalanceHealthChecks(builder.Configuration);

        return builder;
    }

    /// <summary>Monta o Swagger (conforme flag), o pipeline de middleware (§4.4/§8.2) e os endpoints.</summary>
    public static WebApplication UseBalanceApi(this WebApplication app)
    {
        var features = app.Services.GetRequiredService<IOptions<FeatureFlags>>().Value;
        var nonProduction = !app.Environment.IsProduction();

        if (features.Swagger ?? nonProduction)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseAuthentication();
        app.UseMiddleware<CorrelationMiddleware>();
        app.UseAuthorization();

        app.MapControllers();
        app.MapBalanceHealthChecks();

        return app;
    }

    /// <summary>
    /// Resolve as opções de JWT: respeita a config, cai para a variável de ambiente
    /// e só usa a chave default em Development (§9.1) — fora de Development a chave é
    /// obrigatória. Registra o <see cref="JwtOptions"/> para o resto do app.
    /// </summary>
    private static JwtOptions ResolveJwt(WebApplicationBuilder builder)
    {
        var jwt = new JwtOptions();
        builder.Configuration.GetSection(JwtOptions.SectionName).Bind(jwt);

        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
        {
            jwt.SigningKey = Environment.GetEnvironmentVariable("BALANCE_JWT_SIGNING_KEY")
                ?? (builder.Environment.IsDevelopment()
                    ? DevSigningKey
                    : throw new InvalidOperationException("JWT signing key is required outside Development (BALANCE_JWT_SIGNING_KEY)."));
        }

        builder.Services.Configure<JwtOptions>(options =>
        {
            options.Issuer = jwt.Issuer;
            options.Audience = jwt.Audience;
            options.SigningKey = jwt.SigningKey;
        });

        return jwt;
    }
}
