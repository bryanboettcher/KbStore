namespace KbStore.Storefront.Services.Extensions;

using KbStore.Storefront.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public static void AddStorefrontServices(this IServiceCollection services)
    {
        services.AddScoped<ISellableItemCommandService, SellableItemCommandService>();
        services.AddScoped<ISellableItemQueryService, SellableItemQueryService>();

        services.AddTransient<Func<DateTimeOffset>>(_ => () => DateTimeOffset.UtcNow);
    }
}
