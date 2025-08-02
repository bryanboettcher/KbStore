
namespace KbStore.Catalog;

using Extensions;
using KbStore.ServiceDefaults;
using Persistence;


public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        builder.AddNpgsqlDbContext<ApplicationDbContext>("pgsql");
        builder.AddMassTransit();

        var app = builder.Build();
        await app.RunAsync();
    }
}