namespace KbStore.Storefront.Extensions;

using MassTransit;


public static class HostBuilderExtensions
{
    public static void AddMassTransit(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMassTransit(bus =>
        {
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
