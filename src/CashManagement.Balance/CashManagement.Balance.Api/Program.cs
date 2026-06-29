using CashManagement.Balance.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.AddBalanceApi();

var app = builder.Build();
app.UseBalanceApi();

app.Run();

/// <summary>Ponto de entrada exposto para os testes de integração.</summary>
public partial class Program;
