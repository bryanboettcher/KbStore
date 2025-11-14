namespace KbStore.ApiService.Extensions;

using Catalog.Services.Extensions;
using MassTransit;
using Storefront.Services.Extensions;


public static class WebApplicationBuilderExtensions
{
    public static void AddApplicationServices(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var services = builder.Services;

        services.AddCatalogServices(config);
        services.AddStorefrontServices();

        services.AddMassTransit(bus =>
        {
            bus.AddConsumers(typeof(Program).Assembly);

            bus.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(config.GetConnectionString("queue"));
                cfg.ConfigureEndpoints(ctx);
            });
        });
    }
}
