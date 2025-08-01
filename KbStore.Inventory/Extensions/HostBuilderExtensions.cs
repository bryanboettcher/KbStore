namespace KbStore.Inventory.Extensions;

using Domains.StockItems;
using MassTransit;
using Persistence;

public static class HostBuilderExtensions
{
    public static void AddMassTransit(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMassTransit(bus =>
        {
            bus.AddSagaStateMachine<StockItemStateMachine, StockItemEntity>()
                .EntityFrameworkRepository(repo =>
                {
                    repo.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    repo.ExistingDbContext<ApplicationDbContext>();
                    
                    repo.UsePostgres();
                });

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
}