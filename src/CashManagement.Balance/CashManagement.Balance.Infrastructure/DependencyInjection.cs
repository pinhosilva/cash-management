using CashManagement.Balance.Application.Features.BalanceProjection;
using CashManagement.Balance.Application.Features.GetDailyBalance;
using CashManagement.Balance.Infrastructure.Messaging;
using CashManagement.Balance.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace CashManagement.Balance.Infrastructure;

/// <summary>
/// Wire-up de DI da Infrastructure do Balance: MongoDB (read model + projeção),
/// o handler de projeção e o consumer Kafka (hosted service). Lê tudo da
/// <see cref="IConfiguration"/> (Mongo e Kafka), sem segredo embutido.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBalanceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoOptions>(configuration.GetSection(MongoOptions.SectionName));
        services.Configure<KafkaConsumerOptions>(configuration.GetSection(KafkaConsumerOptions.SectionName));

        services.AddSingleton<IMongoClient>(sp =>
        {
            var options = sp.GetMongoOptions();
            return new MongoClient(options.ConnectionString);
        });

        services.AddSingleton(sp =>
        {
            var options = sp.GetMongoOptions();
            var database = sp.GetRequiredService<IMongoClient>().GetDatabase(options.Database);
            return database.GetCollection<DailyBalanceDocument>(options.DailyBalanceCollection);
        });

        services.AddScoped<IDailyBalanceProjection, MongoDailyBalanceProjection>();
        services.AddScoped<IDailyBalanceReader, MongoDailyBalanceReader>();
        services.AddScoped<CreditPostedEventHandler>();

        services.AddHostedService<KafkaConsumerService>();

        return services;
    }

    private static MongoOptions GetMongoOptions(this IServiceProvider sp) =>
        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoOptions>>().Value;
}
