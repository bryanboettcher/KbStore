
namespace KbStore.Services;

using Extensions;
using Persistence;


public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        builder.AddNpgsqlDbContext<ApplicationDbContext>("pgsql");
        builder.AddMassTransit();

        // Add services to the container.
        builder.Services.AddAuthorization();
        
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        
        await app.RunAsync();
    }
}