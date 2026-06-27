using CashManagement.Entries.Api.Auth;
using CashManagement.Entries.Api.Correlation;
using CashManagement.Entries.Api.Http;
using CashManagement.Entries.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Formatting.Compact;

namespace CashManagement.Entries.Api.Configuration;

/// <summary>
/// Composition root da API: concentra o wiring que ficava espalhado no
/// <c>Program.cs</c> em dois pontos navegáveis — <see cref="AddEntriesApi"/>
/// (logging + configuração + registro de serviços) e <see cref="UseEntriesApi"/>
/// (schema de dev + pipeline + endpoints). O <c>Program.cs</c> fica mínimo.
/// </summary>
public static class HostingExtensions
{
    private const string DefaultConnectionString =
        "Server=localhost;Database=CashManagementEntries;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string DevSigningKey = "dev-signing-key-change-me-please-32-bytes-minimum";

    /// <summary>Registra logging estruturado, configuração e todos os serviços da API.</summary>
    public static WebApplicationBuilder AddEntriesApi(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("service", "CashManagement.Entries")
            .Enrich.WithProperty("environment", context.HostingEnvironment.EnvironmentName)
            .WriteTo.Console(new CompactJsonFormatter()));

        var configuration = builder.Configuration;
        var connectionString = configuration.GetConnectionString("Entries") ?? DefaultConnectionString;
        var kafkaBootstrap = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var jwt = ResolveJwt(builder);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(connectionString, kafkaBootstrap);
        builder.Services.AddCorrelation();
        builder.Services.AddJwtAuth(jwt);

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<EntriesDbContext>("entries-db", tags: ["ready"]);

        return builder;
    }

    /// <summary>Monta o schema em dev, o pipeline de middleware (§4.4/§8.2) e os endpoints.</summary>
    public static WebApplication UseEntriesApi(this WebApplication app)
    {
        // Garante o schema em ambientes não-produtivos (em produção usa-se migrations).
        if (!app.Environment.IsProduction())
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<EntriesDbContext>().Database.EnsureCreated();

            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseAuthentication();
        app.UseMiddleware<CorrelationMiddleware>();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
        });

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
            jwt.SigningKey = Environment.GetEnvironmentVariable("ENTRIES_JWT_SIGNING_KEY")
                ?? (builder.Environment.IsDevelopment()
                    ? DevSigningKey
                    : throw new InvalidOperationException("JWT signing key is required outside Development (ENTRIES_JWT_SIGNING_KEY)."));
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
