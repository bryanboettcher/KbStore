namespace KbStore.Catalog.Services.Extensions;

using KbStore.Catalog.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence;


public static class ServiceCollectionExtensions
{
    public static void AddCatalogServices(this IServiceCollection services, ConfigurationManager config)
    {
        services.AddNpgsql<ApplicationDbContext>(config.GetConnectionString("catalog"));

        services.AddScoped<IInventoryCommandService, MassTransitInventoryCommandService>();
        services.AddScoped<IInventoryQueryService, DbContextInventoryQueryService>();

        services.AddScoped<IProductCommandService, MassTransitProductCommandService>();
        services.AddScoped<IProductQueryService, DbContextProductQueryService>();

        services.AddTransient<Func<DateTimeOffset>>(_ => () => DateTimeOffset.UtcNow);
    }
}