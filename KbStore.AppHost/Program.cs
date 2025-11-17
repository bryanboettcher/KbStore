using Aspire.Hosting;
using Projects;

namespace KbStore.AppHost;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var broker = builder.AddRabbitMQ("queue")
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent)
            .WithManagementPlugin();

        var pgsql = builder.AddPostgres("pgsql")
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent)
            .WithPgWeb(conf => conf.WithHostPort(5050));

        var databaseCatalog = pgsql
            .AddDatabase("catalog");

        builder.AddProject<KbStore_Catalog>("domain-catalog")
            .WithReference(broker)
            .WithReference(databaseCatalog);

        var mongo = builder.AddMongoDB("mongo")
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent)
            .WithMongoExpress();

        var databaseStorefront = mongo
            .AddDatabase("storefront");

        builder.AddProject<KbStore_Storefront>("domain-storefront")
            .WithReference(broker)
            .WithReference(databaseStorefront);

        var webApi = builder.AddProject<KbStore_ApiService>("api")
            .WithReference(databaseCatalog)
            .WithExternalHttpEndpoints()
            .WithReference(broker);

        var app = builder.Build();

        await app.RunAsync()
            .ConfigureAwait(false);
    }
}