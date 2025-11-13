namespace KbStore.Storefront.Extensions;

using KbStore.Storefront.Abstractions.Constants;
using KbStore.Storefront.Domains.SellableItems;
using MassTransit;
using MongoDB.Driver;


public static class HostBuilderExtensions
{
    public static void AddMassTransit(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMassTransit(bus =>
        {
            ConfigureSagas(bus);

            bus.UsingRabbitMq(ConfigureQueue);
        });

        return;

        void ConfigureQueue(IBusRegistrationContext ctx, IRabbitMqBusFactoryConfigurator cfg)
        {
            var connectionString = builder.Configuration.GetConnectionString("queue");
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            var uri = new Uri(connectionString);

            cfg.Host(uri);

            cfg.ConfigureEndpoints(ctx);
        }
    }

    private static void ConfigureSagas(IBusRegistrationConfigurator bus)
    {
        bus.AddSagaStateMachine<Domains.SellableItems.SellableItemStateMachine, Domains.SellableItems.SellableItemEntity>()
            .MongoDbRepository(r =>
            {
                r.DatabaseFactory(provider => provider.GetRequiredService<IMongoDatabase>());
                r.CollectionName = Abstractions.Constants.CollectionNames.SellableItems;
            });
    }

    public static async Task EnsureMongoDbIndexesAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();

        var collection = database.GetCollection<SellableItemEntity>(CollectionNames.SellableItems);

        var indexKeys = Builders<SellableItemEntity>.IndexKeys.Ascending(x => x.SKU);
        var indexOptions = new CreateIndexOptions { Unique = true };
        var indexModel = new CreateIndexModel<SellableItemEntity>(indexKeys, indexOptions);

        await collection.Indexes.CreateOneAsync(indexModel);
    }
}
