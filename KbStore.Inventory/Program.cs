
namespace KbStore.Inventory;

using Extensions;
using Persistence;
using ServiceDefaults;


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