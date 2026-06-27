using CashManagement.Entries.Api.Auth;
using CashManagement.Entries.Api.Configuration;
using CashManagement.Entries.Api.Correlation;
using CashManagement.Entries.Api.Http;
using CashManagement.Entries.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Log estruturado em JSON com correlationId/component injetados via LogContext (§8.2).
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("service", "CashManagement.Entries")
    .Enrich.WithProperty("environment", context.HostingEnvironment.EnvironmentName)
    .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter()));

var configuration = builder.Configuration;

var connectionString = configuration.GetConnectionString("Entries")
    ?? "Server=localhost;Database=CashManagementEntries;Trusted_Connection=True;TrustServerCertificate=True;";
var kafkaBootstrap = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

// Chave de assinatura do JWT vem de variável de ambiente (§9.1); default só p/ Dev/testes.
var jwt = new JwtOptions();
configuration.GetSection(JwtOptions.SectionName).Bind(jwt);
if (string.IsNullOrWhiteSpace(jwt.SigningKey))
{
    jwt.SigningKey = Environment.GetEnvironmentVariable("ENTRIES_JWT_SIGNING_KEY")
        ?? (builder.Environment.IsProduction()
            ? throw new InvalidOperationException("JWT signing key is required in production (ENTRIES_JWT_SIGNING_KEY).")
            : "dev-signing-key-change-me-please-32-bytes-minimum");
}

builder.Services.Configure<JwtOptions>(options =>
{
    options.Issuer = jwt.Issuer;
    options.Audience = jwt.Audience;
    options.SigningKey = jwt.SigningKey;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString, kafkaBootstrap);
builder.Services.AddCorrelation();
builder.Services.AddJwtAuth(jwt);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<EntriesDbContext>("entries-db", tags: ["ready"]);

var app = builder.Build();

// Garante o schema em ambientes não-produtivos (em produção usa-se migrations).
if (!app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EntriesDbContext>();
    await db.Database.EnsureCreatedAsync();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseMiddleware<CorrelationMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.Run();

/// <summary>Ponto de entrada exposto para o <c>WebApplicationFactory</c> dos testes de integração.</summary>
public partial class Program;
