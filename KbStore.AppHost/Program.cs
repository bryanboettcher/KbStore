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

        EnlistInventory(builder, broker);
        EnlistStorefront(builder, broker);

        var webApi = builder.AddProject<KbStore_ApiService>("api")
            .WithExternalHttpEndpoints()
            .WithReference(broker).WithParentRelationship(broker);

        var app = builder.Build();

        await app.RunAsync();
    }
    
    private static void EnlistInventory(IDistributedApplicationBuilder builder, IResourceBuilder<RabbitMQServerResource> broker)
    {
        var pgsql = builder.AddPostgres("pgsql")
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent)
            .WithPgWeb(conf => conf.WithHostPort(5050));

        var database = pgsql
            .AddDatabase("inventory");

        builder.AddProject<KbStore_Inventory>("domain-inventory")
            .WithReference(broker).WithParentRelationship(broker)
            .WithReference(database).WithParentRelationship(database);
    }

    private static void EnlistStorefront(IDistributedApplicationBuilder builder, IResourceBuilder<RabbitMQServerResource> broker)
    {
        var mongo = builder.AddMongoDB("mongo")
                .WithDataVolume()
                .WithLifetime(ContainerLifetime.Persistent)
                .WithMongoExpress();

        var database = mongo
            .AddDatabase("storefront");

        builder.AddProject<KbStore_Storefront>("domain-storefront")
            .WithReference(broker).WithParentRelationship(broker)
            .WithReference(database).WithParentRelationship(database);
    }
}