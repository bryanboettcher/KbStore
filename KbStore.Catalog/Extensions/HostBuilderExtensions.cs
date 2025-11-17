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
        var connectionString = builder.Configuration.GetConnectionString("queue");
        
        builder.Services.AddMassTransit(bus =>
        {
            bus.AddHangfireConsumers();
            bus.AddPublishMessageScheduler();

            ConfigureFeatures(bus);
            ConfigureJobConsumers(bus);
            ConfigureSagas(bus);

            ConfigureTransport(bus, connectionString);
        });
    }

    private static void ConfigureTransport(IBusRegistrationConfigurator bus, string? connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var uri = new Uri(connectionString);

        bus.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(uri);

            cfg.UseInMemoryOutbox(ctx, _ => { });
            cfg.UsePublishMessageScheduler();
            cfg.ConfigureEndpoints(ctx);
        });
    }

    private static void ConfigureFeatures(IBusRegistrationConfigurator bus)
    {
        bus.AddConsumers(typeof(Program).Assembly);
        bus.AddFutures(typeof(Program).Assembly);
        bus.AddActivities(typeof(Program).Assembly);
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