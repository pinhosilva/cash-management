using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.IntegrationTests.Api;

/// <summary>
/// Testes dos feature flags (seção <c>Features</c>). Rodam sem container: o flag
/// <c>AutoCreateSchema=false</c> evita tocar o banco, isolando o comportamento do
/// flag sob teste.
/// </summary>
public class FeatureFlagsTests
{
    private static WebApplicationFactory<Program> CreateApi(string? devTokenFlag) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseEnvironment("Development");
            host.UseSetting("Jwt:SigningKey", "feature-flags-test-signing-key-32-bytes!!");
            host.UseSetting("Features:AutoCreateSchema", "false"); // não cria schema: este teste não toca o banco
            if (devTokenFlag is not null)
            {
                host.UseSetting("Features:DevTokenEndpoint", devTokenFlag);
            }
        });

    [Fact]
    public async Task Dev_token_endpoint_is_available_by_default_in_development()
    {
        using var api = CreateApi(devTokenFlag: null);
        using var client = api.CreateClient();

        var response = await client.GetAsync("/dev/token");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Dev_token_endpoint_returns_404_when_the_flag_disables_it()
    {
        using var api = CreateApi(devTokenFlag: "false");
        using var client = api.CreateClient();

        var response = await client.GetAsync("/dev/token");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
