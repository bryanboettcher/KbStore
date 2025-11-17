namespace KbStore.Storefront.Services.Extensions;

using KbStore.Storefront.Abstractions.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;


public static class ServiceCollectionExtensions
{
    public static void AddStorefrontServices(this IServiceCollection services, ConfigurationManager config)
    {
        var connectionString = config.GetConnectionString("storefront");
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString, "storefront connection string");

        var mongoUrl = MongoUrl.Create(connectionString);
        var databaseName = mongoUrl.DatabaseName ?? "storefront";

        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(databaseName);
        });

        services.AddScoped<ISellableItemCommandService, SellableItemCommandService>();
        services.AddScoped<ISellableItemQueryService, SellableItemQueryService>();

        services.AddTransient<Func<DateTimeOffset>>(_ => () => DateTimeOffset.UtcNow);
    }
}
