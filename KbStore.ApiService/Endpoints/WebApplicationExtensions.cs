namespace KbStore.ApiService.Endpoints;

using Catalog;

public static class WebApplicationExtensions
{
    public static void MapApplicationEndpoints(this WebApplication app)
    {
        InventoryEndpoints.MapTo(app);
        ProductEndpoints.MapTo(app);
    }
}