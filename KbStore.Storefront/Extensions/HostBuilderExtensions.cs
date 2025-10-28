namespace KbStore.Storefront.Extensions;

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
        // Saga registrations will be added in Phase 1
        // Example pattern:
        // bus.AddSagaStateMachine<SellableItemStateMachine, SellableItemEntity>()
        //     .MongoDbRepository(r =>
        //     {
        //         r.DatabaseFactory(provider => provider.GetRequiredService<IMongoDatabase>());
        //         r.CollectionName = CollectionNames.SellableItems;
        //     });
    }
}
