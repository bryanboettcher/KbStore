namespace KbStore.Catalog.Extensions;

using Domains.Inventory;
using Domains.Products;
using Hangfire;
using Hangfire.PostgreSql;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Persistence;


public static class HostBuilderExtensions
{
    public static void AddHangfire(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHangfire(conf =>
        {
            var connectionString = builder.Configuration.GetConnectionString("catalog");

            conf.UsePostgreSqlStorage(opt =>
            {
                opt.UseNpgsqlConnection(connectionString);
            });
        });
    }

    public static void AddMassTransit(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMassTransit(bus =>
        {
            bus.AddHangfireConsumers();
            bus.AddPublishMessageScheduler();

            ConfigureJobConsumers(bus);
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
            cfg.UsePublishMessageScheduler();
            cfg.ConfigureEndpoints(ctx);
        }
    }

    private static void ConfigureSagas(IBusRegistrationConfigurator bus)
    {
        bus.AddSagaStateMachine<InventoryStateMachine, InventoryEntity>()
            .EntityFrameworkRepository(repo =>
            {
                repo.ConcurrencyMode = ConcurrencyMode.Optimistic;
                repo.ExistingDbContext<ApplicationDbContext>();
                repo.UsePostgres();
            });

        bus.AddSagaStateMachine<ProductStateMachine, ProductEntity>()
            .EntityFrameworkRepository(repo =>
            {
                repo.ConcurrencyMode = ConcurrencyMode.Optimistic;
                repo.ExistingDbContext<ApplicationDbContext>();
                repo.UsePostgres();
            });
    }

    private static void ConfigureJobConsumers(IBusRegistrationConfigurator bus)
    {
        bus.SetJobConsumerOptions();
        bus.AddJobSagaStateMachines()
            .EntityFrameworkRepository(repo =>
            {
                repo.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                repo.ExistingDbContext<JobServiceSagaDbContext>();
                repo.UsePostgres();
            });
    }
}