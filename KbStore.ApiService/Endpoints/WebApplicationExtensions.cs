namespace KbStore.ApiService.Endpoints;

using Inventory;


public static class WebApplicationExtensions
{
    public static void MapApplicationEndpoints(this WebApplication app)
    {
        InventoryEndpoints.MapTo(app);
    }
}
