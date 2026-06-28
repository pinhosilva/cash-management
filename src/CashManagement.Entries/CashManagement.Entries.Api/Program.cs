using CashManagement.Entries.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.AddEntriesApi();

var app = builder.Build();
app.UseEntriesApi();

app.Run();

/// <summary>Ponto de entrada exposto para o <c>WebApplicationFactory</c> dos testes de integração.</summary>
public partial class Program;
