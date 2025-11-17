namespace KbStore.Catalog.Extensions;

using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Persistence;


public static class WebApplicationExtensions
{
    public static async Task RunMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        await using var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await applicationDbContext.Database.MigrateAsync();

        await using var jobSagaDbContext = scope.ServiceProvider.GetRequiredService<JobServiceSagaDbContext>();
        await jobSagaDbContext.Database.MigrateAsync();
    }
}
