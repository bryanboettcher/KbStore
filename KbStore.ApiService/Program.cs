namespace KbStore.ApiService;

using Endpoints;
using Extensions;
using Scalar.AspNetCore;
using ServiceDefaults;


public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire client integrations.
        builder.AddServiceDefaults();
        builder.AddApplicationServices();

        // Add services to the container.
        builder.Services.AddProblemDetails();
        
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