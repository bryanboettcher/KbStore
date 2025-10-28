namespace KbStore.Storefront;

using Extensions;
using ServiceDefaults;


public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.AddMongoDBClient("storefront");

        builder.AddMassTransit();

        var app = builder.Build();
        await app.RunAsync();
    }
}