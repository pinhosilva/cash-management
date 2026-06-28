using CashManagement.Balance.Infrastructure;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Log estruturado (Serilog), espelhando o Entries: campo "component" e "correlationId"
// vêm dos scopes do consumer/projeção (§8.2), sem PII/segredo.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("service", "CashManagement.Balance")
    .Enrich.WithProperty("environment", context.HostingEnvironment.EnvironmentName)
    .WriteTo.Console(new CompactJsonFormatter()));

// T09: só o host + o consumer (hosted service) + a projeção no Mongo.
// O endpoint GET /balances/{date} é a T10.
builder.Services.AddBalanceInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "CashManagement.Balance");

app.Run();

/// <summary>Ponto de entrada exposto para os testes de integração.</summary>
public partial class Program;
