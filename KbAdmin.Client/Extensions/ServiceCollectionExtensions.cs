using KbAdmin.Client.Configuration;
using KbAdmin.Client.Services;
using Microsoft.Extensions.Options;

namespace KbAdmin.Client.Extensions;

public static class ServiceCollectionExtensions
{ public static void AddKiotaServices(this IServiceCollection services)
    {
        // Add Kiota handlers to the dependency injection container
        services.AddKiotaHandlers();

        // Register the factory for the GitHub client
        services.AddHttpClient<KbStoreClientFactory>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ApiConnectionOptions>>().Value;

            client.BaseAddress = options.ApiRoot;
        }).AttachKiotaHandlers();

        services.AddTransient(sp => sp.GetRequiredService<KbStoreClientFactory>().GetClient());
    }

    public static void AddApiConnectionOptions(this IServiceCollection services, Action<ApiConnectionOptions>? configure = null)
    {
        var builder = services.AddOptions<ApiConnectionOptions>();
        if (configure is not null)
            builder.Configure(configure);

        services.ConfigureOptions<ApiConnectionOptionsConfigurator>();
    }
}
