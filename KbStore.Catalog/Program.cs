namespace KbStore.Catalog;

using Extensions;
using Hangfire;
using KbStore.ServiceDefaults;
using MassTransit.EntityFrameworkCoreIntegration;
using Persistence;


public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        builder.AddNpgsqlDbContext<ApplicationDbContext>("catalog");
        builder.AddNpgsqlDbContext<JobServiceSagaDbContext>("catalog");

        builder.AddHangfire();
        builder.AddMassTransit();

        var app = builder.Build();

        app.UseRouting();
        app.UseHangfireDashboard();

        await app.RunMigrationsAsync().ConfigureAwait(false);
        await app.RunAsync().ConfigureAwait(false);
    }
}