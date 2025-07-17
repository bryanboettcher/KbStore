var builder = DistributedApplication.CreateBuilder(args);

var pgsql = builder.AddPostgres("pgsql")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgWeb(conf => conf.WithHostPort(5050));

var pgdb = pgsql
    .AddDatabase("db");

var broker = builder.AddRabbitMQ("queue")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

var backend = builder.AddProject<Projects.KbStore_Services>("backend")
    .WithReference(pgdb)
    .WithReference(broker);

var webApi = builder.AddProject<Projects.KbStore_ApiService>("api")
    .WithExternalHttpEndpoints()
    .WithReference(broker)
    .WaitFor(backend);

var app = builder.Build();
    
app.Run();
