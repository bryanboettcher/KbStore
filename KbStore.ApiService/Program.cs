namespace KbStore.ApiService;

using Endpoints;
using Scalar.AspNetCore;
using ServiceDefaults;


public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire client integrations.
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.Services.AddProblemDetails();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseExceptionHandler();
        app.MapOpenApi();
        
        app.MapScalarApiReference();
        app.MapDefaultEndpoints();
        app.MapApplicationEndpoints();

        await app.RunAsync();
    }
}